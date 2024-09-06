using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games;

public interface ISyncStartGameService
{
    Task<SyncStartState> GetSyncStartState(string gameId, IEnumerable<string> teamIds, CancellationToken cancellationToken);
    Task HandleSyncStartStateChanged(string gameId, CancellationToken cancellationToken);
    Task<SyncStartPlayerStatusUpdate> UpdatePlayerReadyState(string playerId, bool isReady, CancellationToken cancellationToken);
    Task UpdateTeamReadyState(string teamId, bool isReady, CancellationToken cancellationToken);
}

internal class SyncStartGameService : ISyncStartGameService
{
    private readonly IActingUserService _actingUserService;
    private readonly IAppUrlService _appUrlService;
    private readonly BackgroundAsyncTaskContext _backgroundTaskContext;
    private readonly IGameHubService _gameHubBus;
    private readonly ILogger<SyncStartGameService> _logger;
    private readonly IMediator _mediator;
    private readonly INowService _nowService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IStore _store;
    private readonly IBackgroundAsyncTaskQueueService _taskQueue;
    private readonly ITeamService _teamService;

    public SyncStartGameService
    (
        IActingUserService actingUserService,
        IAppUrlService appUrlService,
        BackgroundAsyncTaskContext backgroundTaskContext,
        IGameHubService gameHubBus,
        ILogger<SyncStartGameService> logger,
        IMediator mediator,
        INowService nowService,
        IServiceScopeFactory serviceScopeFactory,
        IStore store,
        IBackgroundAsyncTaskQueueService taskQueue,
        ITeamService teamService
    )
    {
        _actingUserService = actingUserService;
        _appUrlService = appUrlService;
        _backgroundTaskContext = backgroundTaskContext;
        _gameHubBus = gameHubBus;
        _logger = logger;
        _mediator = mediator;
        _nowService = nowService;
        _serviceScopeFactory = serviceScopeFactory;
        _store = store;
        _taskQueue = taskQueue;
        _teamService = teamService;
    }

    // TODO: need to adjust this to only look at the passed team ids (and make SyncStartStateChanged handler correctly identify the syncing players)
    public async Task<SyncStartState> GetSyncStartState(string gameId, IEnumerable<string> teamIds, CancellationToken cancellationToken)
    {
        var nowish = _nowService.Get();
        var game = await _store
            .WithNoTracking<Data.Game>()
            .Select(g => new { g.Id, g.Name, g.RequireSynchronizedStart })
            .SingleAsync(g => g.Id == gameId, cancellationToken);

        var teamPlayers = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == gameId)
            .WhereDateIsEmpty(p => p.SessionEnd)
            .WhereDateIsEmpty(p => p.SessionBegin)
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.ToArray(), cancellationToken);

        // a game and its challenges are "sync start ready" if either of the following are true:
        // - the game IS NOT a sync-start game
        // - the game IS a sync-start game, and all registered players have set their IsReady flag to true.
        // - it has players
        if (!game.RequireSynchronizedStart)
        {
            return new SyncStartState
            {
                Game = new SimpleEntity { Id = game.Id, Name = game.Name },
                Teams = [],
                AllSessionsStarted = false,
                IsReady = true
            };
        }

        // if we have no players, we're not ready to play
        if (!teamPlayers.Any())
        {
            return new SyncStartState
            {
                Game = new SimpleEntity { Id = game.Id, Name = game.Name },
                Teams = Array.Empty<SyncStartTeam>(),
                AllSessionsStarted = false,
                IsReady = false
            };
        }

        var teams = new List<SyncStartTeam>();
        var allTeamsReady = teamPlayers.All(team => team.Value.All(p => p.IsReady));
        var allTeamsSessionStarted = teamPlayers.All(team => team.Value.All(p => p.SessionBegin.IsNotEmpty()));

        return new SyncStartState
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            Teams = teamPlayers.Keys.Select(teamId => new SyncStartTeam
            {
                Id = teamId,
                Name = _teamService.ResolveCaptain(teamPlayers[teamId]).ApprovedName,
                Players = teamPlayers[teamId].Select(p => new SyncStartPlayer
                {
                    Id = p.Id,
                    Name = p.ApprovedName,
                    IsReady = p.IsReady
                }),
                HasStartedSession = teamPlayers[teamId].All(p => p.SessionBegin.IsNotEmpty()),
                IsReady = teamPlayers[teamId].All(p => p.IsReady)
            }),
            AllSessionsStarted = allTeamsSessionStarted,
            IsReady = allTeamsReady
        };
    }

    public async Task HandleSyncStartStateChanged(string gameId, CancellationToken cancellationToken)
    {
        var validationResult = await ValidateSyncStart(gameId, null, cancellationToken);
        _logger.LogInformation($"Sync start state changed for game {gameId}. Can start?: {validationResult.CanStart}");

        // notify listeners about the change
        await _gameHubBus.SendSyncStartGameStateChanged(validationResult.SyncStartState);

        // if we're not ready, 
        if (!validationResult.CanStart)
        {
            _logger.LogInformation($"Can't start sync-start game {gameId}. {validationResult.Players.Count()} players | All ready?: {validationResult.AllPlayersReady} | Anyone started?: {validationResult.HasStartedPlayers}");
            return;
        }

        // NOTE: we use a background service to kick this off, as it's a long-running task. Updates on the status
        // of the game launch are reported via the "Game Hub" (SignalR websocket).
        await _gameHubBus.SendSyncStartGameStarting(validationResult.SyncStartState);
        _backgroundTaskContext.ActingUser = _actingUserService.Get();
        _backgroundTaskContext.AppBaseUrl = _appUrlService.GetBaseUrl();

        await _taskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            using var scope = _serviceScopeFactory.CreateAsyncScope();
            var teamIds = validationResult.Players.Select(p => p.TeamId).ToArray();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new StartTeamSessionsCommand(teamIds), cancellationToken);
        });
    }

    /// <summary>
    /// Initiates a synchronized game session for all players registered for the given game ID. Optionally offsets
    /// the session length by a countdown in order to give players a little warning that the session is beginning.
    /// </summary>
    /// <returns></returns>
    // public async Task<SyncStartGameStartedState> StartSynchronizedSession(IEnumerable<string> teamIds, CancellationToken cancellationToken)
    // {
    //     _logger.LogInformation($"Starting a synchronized session for teams {string.Join(',', teamIds)}...");
    //     var gameId = await _teamService.GetGameId(teamIds, cancellationToken);
    //     _logger.LogInformation($"Teams are from game {gameId}.");

    //     using (await _lockService.GetSyncStartGameLock(gameId).LockAsync(cancellationToken))
    //     {
    //         _logger.LogInformation($"Acquired asynchronous lock for sync start game {gameId}.");

    //         // validate that the various conditions we need in order to sync start a game are available:
    //         _logger.LogInformation($"Validating sync start for game {gameId}.");
    //         var validateStartResult = await ValidateSyncStart(gameId, teamIds, cancellationToken);

    //         if (!validateStartResult.CanStart)
    //             throw new CantStartSynchronizedSession(gameId, validateStartResult);

    //         // now that the quitters (and by quitters I mean attempts to start a sync game that is already started) 
    //         // are gone, notify signalR that it's business time.
    //         await _gameHubBus.SendSyncStartGameStarting(validateStartResult.SyncStartState);

    //         // validation is all clear - compute new session times and update players, challenges, and gamespaces
    //         _logger.LogInformation($"Starting synchronized session for game {gameId}...");
    //         var startResult = await _mediator.Send(new StartTeamSessionsCommand(teamIds, true), cancellationToken);
    //         var session = startResult.SessionWindow;
    //         _logger.LogInformation($"Synchronized session started for game {gameId}.Start: {session.Start}. End: {startResult.SessionWindow.End}. Total duration: {(session.End - session.Start).TotalMinutes} minutes.");

    //         // compose a return value summarizing the sync session
    //         var startState = new SyncStartGameStartedState
    //         {
    //             Game = new SimpleEntity { Id = validateStartResult.Game.Id },
    //             SessionBegin = startResult.SessionWindow.Start,
    //             SessionEnd = startResult.SessionWindow.End,
    //             Teams = validateStartResult.Players
    //                 .GroupBy(p => p.TeamId)
    //                 .ToDictionary(p => p.Key, p => p.Select(p => new SyncStartGameStartedStatePlayer
    //                 {
    //                     Id = p.Id,
    //                     Name = p.Name,
    //                     UserId = p.UserId
    //                 }))
    //         };

    //         await _gameHubBus.SendSyncStartGameStarted(startState);
    //         _logger.LogInformation($"Synchronized session started for game {gameId}! ({teamIds.Count()} are playing.)");
    //         return startState;
    //     }
    // }

    public async Task<SyncStartPlayerStatusUpdate> UpdatePlayerReadyState(string playerId, bool isReady, CancellationToken cancellationToken)
    {
        var player = await _store
            .WithTracking<Data.Player>()
            .SingleAsync(p => p.Id == playerId, cancellationToken);

        player.IsReady = isReady;
        await _store.SaveUpdate(player, cancellationToken);
        await HandleSyncStartStateChanged(player.GameId, cancellationToken);

        return new SyncStartPlayerStatusUpdate
        {
            Id = player.Id,
            Name = player.ApprovedName,
            GameId = player.GameId,
            IsReady = isReady
        };
    }

    public async Task UpdateTeamReadyState(string teamId, bool isReady, CancellationToken cancellationToken)
    {
        // load with tracking since we need the gameId anyway
        var players = await _store
            .WithTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .ToArrayAsync(cancellationToken);

        var gameIds = players.Select(p => p.GameId).ToArray();

        if (players.Any())
        {
            foreach (var player in players)
                player.IsReady = isReady;
        }

        // update all players
        await _store.SaveUpdateRange(players);

        // update all games for sync start readiness (likely only one game, but you know)
        foreach (var gameId in gameIds)
            await HandleSyncStartStateChanged(gameId, cancellationToken);
    }

    private async Task<ValidateSyncStartResult> ValidateSyncStart(string gameId, IEnumerable<string> teamIds, CancellationToken cancellationToken)
    {
        // make sure we have a legal sync start game
        var game = await _store.WithNoTracking<Data.Game>()
            .Select(g => new ValidateSyncStartGame
            {
                Id = g.Id,
                Name = g.Name,
                IsSyncStart = g.RequireSynchronizedStart,
                ExecutionWindow = new DateRange
                {
                    Start = g.GameStart,
                    End = g.GameEnd
                },
                SessionMinutes = g.SessionMinutes
            })
            .SingleAsync(g => g.Id == gameId, cancellationToken);

        if (!game.IsSyncStart)
            throw new CantSynchronizeNonSynchronizedGame(gameId);

        var state = await GetSyncStartState(gameId, teamIds, cancellationToken);

        // ensure no one has already started - if they have, things will get gnarly quick  
        //
        // currently, we don't have an authoritative "This is the session time of this game" kind of construct in the modeling layer
        // instead, we look at the minimum session start already set. this should be the min value for new games. 
        var fauxTeamIds = state.Teams.Select(t => t.Id).ToArray();
        var players = await _store
            .WithNoTracking<Data.Player>()
            // hack until we really use teamids as intended
            .Where(p => fauxTeamIds.Contains(p.TeamId))
            .Select(p => new ValidateSyncStartResultPlayer
            {
                Id = p.Id,
                Name = string.IsNullOrEmpty(p.ApprovedName) ? p.Name : p.ApprovedName,
                IsReady = p.IsReady,
                SessionBegin = p.SessionBegin,
                SessionEnd = p.SessionEnd,
                TeamId = p.TeamId,
                UserId = p.UserId
            }).ToArrayAsync(cancellationToken);

        // just for clarity, the game can start when:
        // - "now" is inside the game execution window
        // - there are a nonzero number of players
        // - all players have marked "ready" (or had it marked for them by an admin)
        // - the game doesn't contain any players with started sessions
        // - all the player sessions are aligned
        var allPlayersReady = players.All(p => p.IsReady);
        var hasStartedPlayers = players.Any(p => p.SessionBegin.IsNotEmpty() || p.SessionEnd.IsNotEmpty());
        var nowish = _nowService.Get();
        var isInExecutionWindow =
        (
            (game.ExecutionWindow.Start.IsEmpty() || game.ExecutionWindow.Start <= nowish) &&
            (game.ExecutionWindow.End.IsEmpty() || game.ExecutionWindow.End >= nowish)
        );

        // if the game is started, or if any players have sessions, don't start,
        return new ValidateSyncStartResult
        {
            CanStart = players.Length > 0 && allPlayersReady && !hasStartedPlayers,
            Game = game,
            AllPlayersReady = allPlayersReady,
            HasStartedPlayers = hasStartedPlayers,
            IsInExecutionWindow = isInExecutionWindow,
            Players = players,
            SyncStartState = state
        };
    }
}
