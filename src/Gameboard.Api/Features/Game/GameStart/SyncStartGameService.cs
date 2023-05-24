using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public interface ISyncStartGameService
{
    Task<SyncStartState> GetSyncStartState(string gameId);
    Task<SynchronizedGameStartedState> StartSynchronizedSession(string gameId, double countdownSeconds = 15);
    Task<SyncStartPlayerStatusUpdate> UpdatePlayerReadyState(string playerId, bool isReady);
}

internal class SyncStartGameService : ISyncStartGameService
{
    private readonly IGameHubBus _gameHub;
    private readonly IGameStore _gameStore;
    private readonly ILockService _lockService;
    private readonly IPlayerStore _playerStore;

    public SyncStartGameService
    (
        IGameHubBus gameHub,
        IGameStore gameStore,
        ILockService lockService,
        IPlayerStore playerStore
    )
    {
        _gameHub = gameHub;
        _lockService = lockService;
        _playerStore = playerStore;
        _gameStore = gameStore;
    }

    public async Task<SyncStartState> GetSyncStartState(string gameId)
    {
        var game = await _gameStore.Retrieve(gameId);

        // a game and its challenges are "sync start ready" if either of the following are true:
        // - the game IS NOT a sync-start game
        // - the game IS sync-start game, and all registered players have set their IsReady flag to true.
        if (!game.RequireSynchronizedStart)
        {
            return new SyncStartState
            {
                Game = new SimpleEntity { Id = game.Id, Name = game.Name },
                Teams = new SyncStartTeam[] { },
                IsReady = true
            };
        }

        // TODO: for some reason, clever uses of groupby and todictionaryasync aren't working like i expect them to.
        // they have stale properties. For example, compare the IsReady property of playersDebug here to the result of allTeamsReady
        // var playersDebug = await _playerStore
        //     .List()
        //     .Where(p => p.GameId == gameId)
        //     .Select(p => new
        //     {
        //         Id = p.Id,
        //         IsReady = p.IsReady
        //     })
        //     .ToListAsync();

        // var teams = new List<SyncStartTeam>();
        // var teamPlayers = await _playerStore
        //     .List()
        //     .Where(p => p.GameId == gameId)
        //     .GroupBy(p => p.TeamId)
        //     .ToDictionaryAsync(tp => tp.Key, tp => tp.ToList());
        // var allTeamsReady = teamPlayers.All(team => team.Value.All(p => p.IsReady));

        // out of time, so for now, manually group on returned players
        var players = await _playerStore
            .List()
            .AsNoTracking()
            .Where(p => p.GameId == gameId)
            .ToListAsync();

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
                Name = teamPlayers[teamId].Single(p => p.Role == PlayerRole.Manager).ApprovedName,
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

    /// <summary>
    /// Initiates a synchronized game session for all players registered for the given game ID. Optionally offsets
    /// the session length by a countdown in order to give players a little warning that the session is beginning.
    /// </summary>
    /// <param name="gameId">The id of the game to start.</param>
    /// <param name="countdownSeconds">
    ///     Number of seconds between the current time and the start of the session (used to display a countdown in clients.
    /// </param>
    /// <returns></returns>
    /// <exception cref="CantSynchronizeNonSynchronizedGame">This call fails if the game is not marked as `RequiresSyncStart`.</exception>
    /// <exception cref="CantStartNonReadySynchronizedGame">This call fails if initiated before all players have set their `IsReady` to true.</exception>
    /// <exception cref="SynchronizedGameHasPlayersWithSessionsBeforeStart">This call fails if any players already have an active game session when it starts.</exception>
    public async Task<SynchronizedGameStartedState> StartSynchronizedSession(string gameId, double countdownSeconds = 15)
    {
        using (await _lockService.GetSyncStartGameLock(gameId).LockAsync())
        {
            // make sure we have a legal sync start game
            var game = await _gameStore.Retrieve(gameId);

            if (!game.RequireSynchronizedStart)
                throw new CantSynchronizeNonSynchronizedGame(gameId);

            var state = await GetSyncStartState(gameId);
            if (!state.IsReady)
                throw new CantStartNonReadySynchronizedGame(state);

            // set the session times for all players
            var players = await _playerStore
                .List()
                .AsNoTracking()
                .Where(p => p.GameId == gameId)
                .Select(p => new
                {
                    Id = p.Id,
                    Name = string.IsNullOrEmpty(p.ApprovedName) ? p.Name : p.ApprovedName,
                    SessionBegin = p.SessionBegin,
                    SessionEnd = p.SessionEnd,
                    TeamId = p.TeamId
                }).ToListAsync();

            // currently, we don't have an authoritative "This is the session time of this game" kind of construct in the modeling layer
            // instead, we look at the minimum session start already set. this should be the min value for new games. if it's this value, 
            // set everyone's who doesn't have a session start to now plus something like 15 sec of lead time. 
            var playersWithSessions = players.Where(p => p.SessionBegin > DateTimeOffset.MinValue || p.SessionEnd > DateTimeOffset.MinValue);
            if (playersWithSessions.Count() > 0)
                throw new SynchronizedGameHasPlayersWithSessionsBeforeStart(game.Id, playersWithSessions.Select(p => p.Id));

            var sessionBegin = DateTimeOffset.UtcNow.AddSeconds(countdownSeconds);
            var sessionEnd = sessionBegin.AddMinutes(game.SessionMinutes);

            await _playerStore
                .List()
                .Where(p => p.GameId == gameId && p.SessionBegin == DateTimeOffset.MinValue)
                .ExecuteUpdateAsync
                (
                    p => p
                        .SetProperty(p => p.SessionBegin, sessionBegin)
                        .SetProperty(p => p.SessionEnd, sessionEnd)
                );


            var startState = new SynchronizedGameStartedState
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

            await _gameHub.SendSyncStartGameStarting(startState);
            return startState;
        }
    }

    public async Task<SyncStartPlayerStatusUpdate> UpdatePlayerReadyState(string playerId, bool isReady)
    {
        var player = await _playerStore.Retrieve(playerId);
        await _playerStore
            .List()
            .Where(p => p.Id == playerId)
            .ExecuteUpdateAsync(p => p.SetProperty(p => p.IsReady, isReady));

        return new SyncStartPlayerStatusUpdate
        {
            Id = player.Id,
            Name = player.ApprovedName,
            GameId = player.GameId,
            IsReady = isReady
        };
    }
}
