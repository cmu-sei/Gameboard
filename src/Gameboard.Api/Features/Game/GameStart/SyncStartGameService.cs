using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games;

public interface ISyncStartGameService
{
    Task<SyncStartState> GetSyncStartState(string gameId, CancellationToken cancellationToken);
    Task HandleSyncStartStateChanged(string gameId, CancellationToken cancellationToken);
    Task<SyncStartGameStartedState> StartSynchronizedSession(string gameId, CalculatedSessionWindow sessionWindow, CancellationToken cancellationToken);
    Task<SyncStartPlayerStatusUpdate> UpdatePlayerReadyState(string playerId, bool isReady, CancellationToken cancellationToken);
    Task UpdateTeamReadyState(string teamId, bool isReady, CancellationToken cancellationToken);
}

internal class SyncStartGameService : ISyncStartGameService
{
    private readonly IActingUserService _actingUserService;
    private readonly IAppUrlService _appUrlService;
    private readonly BackgroundAsyncTaskContext _backgroundTaskContext;
    private readonly IGameHubService _gameHubBus;
    private readonly ILockService _lockService;
    private readonly ILogger<SyncStartGameService> _logger;
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
        ILockService lockService,
        ILogger<SyncStartGameService> logger,
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
        _lockService = lockService;
        _logger = logger;
        _nowService = nowService;
        _serviceScopeFactory = serviceScopeFactory;
        _store = store;
        _taskQueue = taskQueue;
        _teamService = teamService;
    }

    public async Task<SyncStartState> GetSyncStartState(string gameId, CancellationToken cancellationToken)
    {
        var game = await _store
            .WithNoTracking<Data.Game>()
            .Include(g => g.Players)
            .SingleAsync(g => g.Id == gameId, cancellationToken);

        // a game and its challenges are "sync start ready" if either of the following are true:
        // - the game IS NOT a sync-start game
        // - the game IS a sync-start game, and all registered players have set their IsReady flag to true.
        if (!game.RequireSynchronizedStart)
        {
            return new SyncStartState
            {
                Game = new SimpleEntity { Id = game.Id, Name = game.Name },
                Teams = Array.Empty<SyncStartTeam>(),
                AllSessionsStarted = false,
                IsReady = true
            };
        }

        // if we have no players, we're not ready to play
        if (!game.Players.Any())
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
        var teamPlayers = game
            .Players
            .GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.ToList());

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
        var validationResult = await ValidateSyncStart(gameId, cancellationToken);
        _logger.LogInformation($"Sync start state changed for game {gameId}. Can start?: {validationResult.CanStart}");

        // notify listeners about the change
        await _gameHubBus.SendSyncStartGameStateChanged(validationResult.SyncStartState);

        // if we're not ready, 
        if (!validationResult.CanStart)
        {
            _logger.LogInformation($"Can't start sync-start game {gameId}. {validationResult.Players.Count()} | {validationResult.AllPlayersReady} | {validationResult.HasStartedPlayers}");
            return;
        }

        // for now, we're assuming the "happy path" of sync start games being external games, but we'll separate them later
        // NOTE: we also use a background service to kick this off, as it's a long-running task. Updates on the status
        // of the game launch are reported via the SignalR "Game Hub".
        _backgroundTaskContext.ActingUser = _actingUserService.Get();
        _backgroundTaskContext.AppBaseUrl = _appUrlService.GetBaseUrl();

        await _taskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var gameStartService = scope.ServiceProvider.GetRequiredService<IGameStartService>();
            await gameStartService.Start(new GameStartRequest { TeamIds = validationResult.SyncStartState.Teams.Select(t => t.Id).Distinct() }, cancellationToken);
        });
    }

    /// <summary>
    /// Initiates a synchronized game session for all players registered for the given game ID. Optionally offsets
    /// the session length by a countdown in order to give players a little warning that the session is beginning.
    /// </summary>
    /// <param name="gameId">The id of the game to start.</param>
    /// <param name="sessionWindow"></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public async Task<SyncStartGameStartedState> StartSynchronizedSession(string gameId, CalculatedSessionWindow sessionWindow, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Acquiring asynchronous lock for sync start game {gameId}...");
        using (await _lockService.GetSyncStartGameLock(gameId).LockAsync(cancellationToken))
        {
            _logger.LogInformation($"Acquired asynchronous lock for sync start game {gameId}.");

            // validate that the various conditions we need in order to sync start a game are available:
            _logger.LogInformation($"Validating sync start for game {gameId}.");
            var validateStartResult = await ValidateSyncStart(gameId, cancellationToken);

            if (!validateStartResult.CanStart)
                throw new CantStartSynchronizedSession(gameId, validateStartResult);

            // now that the quitters (and by quitters I mean attempts to start a sync game that is already started) 
            // are gone, notify signalR that it's business time.
            await _gameHubBus.SendSyncStartGameStarting(validateStartResult.SyncStartState);

            // validation is all clear - compute new session times and update players, challenges, and gamespaces
            var nowish = _nowService.Get();
            var sessionBegin = sessionWindow.Start;
            var sessionEnd = sessionWindow.End;
            _logger.LogInformation($"Starting synchronized session for game {gameId}. Start: {sessionBegin}. End: {sessionEnd}. Total duration: {(sessionEnd - sessionBegin).TotalMinutes} minutes.");

            var gameTeamIds = validateStartResult.Players.Select(p => p.TeamId).Distinct().ToArray();
            // TODO: combine these into a single DB call in the teams service (passing gameID)
            _logger.LogInformation($"Adjusting session window for {gameTeamIds.Length} teams...");
            foreach (var teamId in gameTeamIds)
            {
                await _teamService.UpdateSessionStartAndEnd(teamId, sessionBegin, sessionEnd, cancellationToken);
            }
            _logger.LogInformation($"Synchronized session started for game {gameId}.");

            // compose a return value summarizing the sync session
            var startState = new SyncStartGameStartedState
            {
                Game = new SimpleEntity { Id = validateStartResult.Game.Id },
                SessionBegin = sessionBegin,
                SessionEnd = sessionEnd,
                Teams = validateStartResult.Players
                    .GroupBy(p => p.TeamId)
                    .ToDictionary(p => p.Key, p => p.Select(p => new SyncStartGameStartedStatePlayer
                    {
                        Id = p.Id,
                        Name = p.Name,
                        UserId = p.UserId
                    }))
            };

            await _gameHubBus.SendSyncStartGameStarted(startState);
            return startState;
        }
    }

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

    private async Task<ValidateSyncStartResult> ValidateSyncStart(string gameId, CancellationToken cancellationToken)
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

        var state = await GetSyncStartState(gameId, cancellationToken);

        // ensure no one has already started - if they have, things will get gnarly quick  
        //
        // currently, we don't have an authoritative "This is the session time of this game" kind of construct in the modeling layer
        // instead, we look at the minimum session start already set. this should be the min value for new games. 
        var players = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == gameId)
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
