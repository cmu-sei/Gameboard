using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public interface ISyncStartGameService
{
    Task<SyncStartState> GetSyncStartState(string gameId, CancellationToken cancellationToken);
    Task HandleSyncStartStateChanged(string gameId, CancellationToken cancellationToken);
    Task<SyncStartGameStartedState> StartSynchronizedSession(string gameId, double countdownSeconds, CancellationToken cancellationToken);
    Task<SyncStartPlayerStatusUpdate> UpdatePlayerReadyState(string playerId, bool isReady, CancellationToken cancellationToken);
}

internal class SyncStartGameService : ISyncStartGameService
{
    private readonly IFireAndForgetService _fireAndForgetService;
    private readonly IGameHubBus _gameHubBus;
    private readonly ILockService _lockService;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public SyncStartGameService
    (
        IFireAndForgetService fireAndForgetService,
        IGameHubBus gameHubBus,
        ILockService lockService,
        IStore store,
        ITeamService teamService
    )
    {
        _fireAndForgetService = fireAndForgetService;
        _gameHubBus = gameHubBus;
        _lockService = lockService;
        _store = store;
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
        // NOTE: we also use a special service to kick this off, because if we don't, the player who initiated the game start
        // won't get a response for several minutes and will likely receive a timeout error. Updates on the status
        // of the game launch are reported via SignalR.
        _fireAndForgetService.Fire<IGameStartService>
        (
            async gameStartService =>
            {
                using (await _lockService.GetFireAndForgetContextLock().LockAsync())
                {
                    await gameStartService.Start(new GameStartRequest { GameId = state.Game.Id }, cancellationToken)
                }
            }
        );
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
        using (await _lockService.GetSyncStartGameLock(gameId).LockAsync())
        {
            // make sure we have a legal sync start game
            var game = await _store.WithNoTracking<Data.Game>().SingleAsync(g => g.Id == gameId);

            if (!game.RequireSynchronizedStart)
                throw new CantSynchronizeNonSynchronizedGame(gameId);

            var state = await GetSyncStartState(gameId, cancellationToken);
            if (!state.IsReady)
                throw new CantStartNonReadySynchronizedGame(state);

            // set the session times for all players
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

            // currently, we don't have an authoritative "This is the session time of this game" kind of construct in the modeling layer
            // instead, we look at the minimum session start already set. this should be the min value for new games. if it's this value, 
            // set everyone's who doesn't have a session start to now plus something like 15 sec of lead time. 
            var playersWithSessions = players.Where(p => p.SessionBegin > DateTimeOffset.MinValue || p.SessionEnd > DateTimeOffset.MinValue);
            if (playersWithSessions.Count() > 0)
                throw new SynchronizedGameHasPlayersWithSessionsBeforeStart(game.Id, playersWithSessions.Select(p => p.Id));

            var sessionBegin = DateTimeOffset.UtcNow.AddSeconds(countdownSeconds);
            var sessionEnd = sessionBegin.AddMinutes(game.SessionMinutes);

            await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.GameId == gameId && p.SessionBegin == DateTimeOffset.MinValue)
                .ExecuteUpdateAsync
                (
                    p => p
                        .SetProperty(p => p.SessionBegin, sessionBegin)
                        .SetProperty(p => p.SessionEnd, sessionEnd)
                );


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

            await _gameHubBus.SendSyncStartGameStarting(startState);
            return startState;
        }
    }

    public async Task<SyncStartPlayerStatusUpdate> UpdatePlayerReadyState(string playerId, bool isReady, CancellationToken cancellationToken)
    {
        var player = await _store.SingleAsync<Data.Player>(playerId, cancellationToken);
        player.IsReady = isReady;
        await _store.SaveUpdate(player, cancellationToken);

        return new SyncStartPlayerStatusUpdate
        {
            Id = player.Id,
            Name = player.ApprovedName,
            GameId = player.GameId,
            IsReady = isReady
        };
    }
}
