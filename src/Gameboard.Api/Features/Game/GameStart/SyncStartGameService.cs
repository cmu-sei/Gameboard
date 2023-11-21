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
using Gameboard.Api.Structure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Games;

public interface ISyncStartGameService
{
    Task<SyncStartState> GetSyncStartState(string gameId, CancellationToken cancellationToken);
    Task HandleSyncStartStateChanged(string gameId, CancellationToken cancellationToken);
    Task<SyncStartGameStartedState> StartSynchronizedSession(string gameId, double countdownSeconds, CancellationToken cancellationToken);
    Task<SyncStartPlayerStatusUpdate> UpdatePlayerReadyState(string playerId, bool isReady, CancellationToken cancellationToken);
    Task UpdateTeamReadyState(string teamId, bool isReady, CancellationToken cancellationToken);
}

internal class SyncStartGameService : ISyncStartGameService
{
    private readonly IExternalGameHostAccessTokenProvider _accessTokenProvider;
    private readonly IActingUserService _actingUserService;
    private readonly IAppUrlService _appUrlService;
    private readonly BackgroundTaskContext _backgroundTaskContext;
    private readonly IGameHubBus _gameHubBus;
    private readonly ILockService _lockService;
    private readonly ILogger<SyncStartGameService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IStore _store;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ITeamService _teamService;

    public SyncStartGameService
    (
        IExternalGameHostAccessTokenProvider accessTokenProvider,
        IActingUserService actingUserService,
        IAppUrlService appUrlService,
        BackgroundTaskContext backgroundTaskContext,
        IGameHubBus gameHubBus,
        ILockService lockService,
        ILogger<SyncStartGameService> logger,
        IServiceScopeFactory serviceScopeFactory,
        IStore store,
        IBackgroundTaskQueue taskQueue,
        ITeamService teamService
    )
    {
        _accessTokenProvider = accessTokenProvider;
        _actingUserService = actingUserService;
        _appUrlService = appUrlService;
        _backgroundTaskContext = backgroundTaskContext;
        _gameHubBus = gameHubBus;
        _lockService = lockService;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _store = store;
        _taskQueue = taskQueue;
        _teamService = teamService;
    }

    public async Task<SyncStartState> GetSyncStartState(string gameId, CancellationToken cancellationToken)
    {
        var game = await _store
            .WithNoTracking<Data.Game>()
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
                IsReady = true
            };
        }

        var players = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.GameId == gameId)
            .ToArrayAsync(cancellationToken);

        // if we have no players, we're not ready to play
        if (!players.Any())
        {
            return new SyncStartState
            {
                Game = new SimpleEntity { Id = game.Id, Name = game.Name },
                Teams = Array.Empty<SyncStartTeam>(),
                IsReady = false
            };
        }

        var teams = new List<SyncStartTeam>();
        var teamPlayers = players
            .GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var allTeamsReady = teamPlayers.All(team => team.Value.All(p => p.IsReady));

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
                IsReady = teamPlayers[teamId].All(p => p.IsReady)
            }),
            IsReady = allTeamsReady
        };
    }

    public async Task HandleSyncStartStateChanged(string gameId, CancellationToken cancellationToken)
    {
        var state = await GetSyncStartState(gameId, cancellationToken);
        await _gameHubBus.SendSyncStartGameStateChanged(state);

        // IFF everyone is ready, start all sessions and return info about them
        if (!state.IsReady)
            return;

        // for now, we're assuming the "happy path" of sync start games being external games, but we'll separate them later
        // NOTE: we also use a background service to kick this off, as it's a long-running task. Updates on the status
        // of the game launch are reported via the SignalR "Game Hub".
        _backgroundTaskContext.AccessToken = await _accessTokenProvider.GetToken();
        _backgroundTaskContext.ActingUser = _actingUserService.Get();
        _backgroundTaskContext.AppBaseUrl = _appUrlService.GetBaseUrl();

        await _taskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var gameStartService = scope.ServiceProvider.GetRequiredService<IGameStartService>();
            await gameStartService.Start(new GameStartRequest { GameId = state.Game.Id }, cancellationToken);
        });
    }

    /// <summary>
    /// Initiates a synchronized game session for all players registered for the given game ID. Optionally offsets
    /// the session length by a countdown in order to give players a little warning that the session is beginning.
    /// </summary>
    /// <param name="gameId">The id of the game to start.</param>
    /// <param name="countdownSeconds">
    ///     Number of seconds between the current time and the start of the session (used to display a countdown in clients.
    /// </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    /// <exception cref="CantSynchronizeNonSynchronizedGame">This call fails if the game is not marked as `RequiresSyncStart`.</exception>
    /// <exception cref="CantStartNonReadySynchronizedGame">This call fails if initiated before all players have set their `IsReady` to true.</exception>
    /// <exception cref="SynchronizedGameHasPlayersWithSessionsBeforeStart">This call fails if any players already have an active game session when it starts.</exception>
    public async Task<SyncStartGameStartedState> StartSynchronizedSession(string gameId, double countdownSeconds, CancellationToken cancellationToken)
    {
        using (await _lockService.GetSyncStartGameLock(gameId).LockAsync(cancellationToken))
        {
            // make sure we have a legal sync start game
            var game = await _store.WithNoTracking<Data.Game>().SingleAsync(g => g.Id == gameId, cancellationToken);

            if (!game.RequireSynchronizedStart)
                throw new CantSynchronizeNonSynchronizedGame(gameId);

            var state = await GetSyncStartState(gameId, cancellationToken);
            if (!state.IsReady)
                throw new CantStartNonReadySynchronizedGame(state);

            // notify signalR
            await _gameHubBus.SendSyncStartGameStarting(state);

            // ensure no one has already started - if they have, things will get gnarly quick  
            //
            // currently, we don't have an authoritative "This is the session time of this game" kind of construct in the modeling layer
            // instead, we look at the minimum session start already set. this should be the min value for new games. if it's this value, 
            // set everyone's who doesn't have a session start to now plus something like 15 sec of lead time. 
            var players = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.GameId == gameId)
                .Select(p => new
                {
                    p.Id,
                    Name = string.IsNullOrEmpty(p.ApprovedName) ? p.Name : p.ApprovedName,
                    p.SessionBegin,
                    p.SessionEnd,
                    p.TeamId
                }).ToListAsync();

            var playersWithSessions = players.Where(p => p.SessionBegin > DateTimeOffset.MinValue || p.SessionEnd > DateTimeOffset.MinValue);
            if (playersWithSessions.Any())
                throw new SynchronizedGameHasPlayersWithSessionsBeforeStart(game.Id, playersWithSessions.Select(p => p.Id));

            // validation is all clear - compute new session times and update players, challenges, and gamespaces
            var sessionBegin = DateTimeOffset.UtcNow.AddSeconds(countdownSeconds);
            var sessionEnd = sessionBegin.AddMinutes(game.SessionMinutes);
            _logger.LogInformation($"Starting synchronized session for game {gameId}. Start: {sessionBegin}. End: {sessionEnd}. Total duration: {(sessionEnd - sessionBegin).TotalMinutes} minutes.");

            var gameTeamIds = players.Select(p => p.TeamId).Distinct().ToArray();
            // TODO: combine these into a single DB call in the teams service (passing gameID)
            foreach (var teamId in gameTeamIds)
            {
                await _teamService.UpdateSessionStartAndEnd(teamId, sessionBegin, sessionEnd, cancellationToken);
            }
            _logger.LogInformation($"Synchronized session started for game {gameId}.");

            // compose a return value summarizing the sync session
            var startState = new SyncStartGameStartedState
            {
                Game = new SimpleEntity { Id = game.Id },
                SessionBegin = sessionBegin,
                SessionEnd = sessionEnd,
                Teams = players
                    .GroupBy(p => p.TeamId)
                    .ToDictionary(p => p.Key, p => p.Select(p => new SimpleEntity
                    {
                        Id = p.Id,
                        Name = p.Name
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

        if (players.Any())
        {
            foreach (var player in players)
                player.IsReady = isReady;

            await _store.SaveUpdateRange(players);
            await HandleSyncStartStateChanged(players.First().GameId, cancellationToken);
        }
    }
}
