using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games;

public interface IGameHubService : INotificationHandler<GameEnrolledPlayersChangeNotification>
{
    // invoke functions on clients
    Task SendExternalGameChallengesDeployStart(GameStartUpdate state);
    Task SendExternalGameChallengesDeployProgressChange(GameStartUpdate state);
    Task SendExternalGameChallengesDeployEnd(GameStartUpdate state);
    Task SendExternalGameLaunchStart(GameStartUpdate state);
    Task SendExternalGameLaunchEnd(GameStartUpdate state);
    Task SendExternalGameLaunchFailure(GameStartUpdate state);
    Task SendExternalGameGamespacesDeployStart(GameStartUpdate state);
    Task SendExternalGameGamespacesDeployProgressChange(GameStartUpdate state);
    Task SendExternalGameGamespacesDeployEnd(GameStartUpdate state);
    Task SendSyncStartGameStateChanged(SyncStartState state);
    Task SendSyncStartGameStarted(SyncStartGameStartedState state);
    Task SendSyncStartGameStarting(SyncStartState state);
    // Task SendYourActiveGamesChanged(string userId);
}

internal class GameHubService : IGameHubService, IGameboardHubService
{
    private static readonly ConcurrentDictionary<string, string[]> _gameIdUserIdsMap = new();

    private readonly IHubContext<GameHub, IGameHubEvent> _hubContext;
    private readonly INowService _now;
    private readonly IStore _store;

    public GameboardHubType GroupType => GameboardHubType.Game;

    public GameHubService
    (
        IHubContext<GameHub, IGameHubEvent> hubContext,
        INowService now,
        IStore store
    )
    {
        _hubContext = hubContext;
        _now = now;
        _store = store;
    }

    public async Task SendExternalGameChallengesDeployStart(GameStartUpdate state)
    {
        await _hubContext
            .Clients
            .Users(GetGameUserIds(state.Game.Id))
            .ExternalGameChallengesDeployStart(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameChallengesDeployStart,
                Data = state
            });
    }

    public Task SendExternalGameChallengesDeployProgressChange(GameStartUpdate state)
        => _hubContext
            .Clients
            .Users(GetGameUserIds(state.Game.Id))
            .ExternalGameChallengesDeployProgressChange(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameChallengesDeployProgressChange,
                Data = state
            });

    public Task SendExternalGameChallengesDeployEnd(GameStartUpdate state)
        => _hubContext
            .Clients
            .Users(GetGameUserIds(state.Game.Id))
            .ExternalGameChallengesDeployEnd(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameChallengesDeployEnd,
                Data = state
            });

    public Task SendExternalGameLaunchStart(GameStartUpdate state)
    {
        return _hubContext
            .Clients
            .Users(GetGameUserIds(state.Game.Id))
            .ExternalGameLaunchStart(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameLaunchStart,
                Data = state
            });
    }

    public Task SendExternalGameLaunchEnd(GameStartUpdate state)
        => _hubContext
            .Clients
            .Users(GetGameUserIds(state.Game.Id))
            .ExternalGameLaunchEnd(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameLaunchStart,
                Data = state
            });

    public Task SendExternalGameLaunchFailure(GameStartUpdate state)
        => _hubContext
            .Clients
            .Users(GetGameUserIds(state.Game.Id))
            .ExternalGameLaunchFailure(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameLaunchFailure,
                Data = state
            });

    public async Task SendExternalGameGamespacesDeployStart(GameStartUpdate state)
    {
        await _hubContext
            .Clients
            .Users(GetGameUserIds(state.Game.Id))
            .ExternalGameGamespacesDeployStart(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameGamespacesDeployStart,
                Data = state
            });
    }

    public async Task SendExternalGameGamespacesDeployProgressChange(GameStartUpdate state)
    {
        await _hubContext
            .Clients
            .Users(GetGameUserIds(state.Game.Id))
            .ExternalGameGamespacesDeployProgressChange(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameGamespacesDeployProgressChange,
                Data = state
            });
    }

    public async Task SendExternalGameGamespacesDeployEnd(GameStartUpdate state)
    {
        await _hubContext
            .Clients
            .Users(GetGameUserIds(state.Game.Id))
            .ExternalGameGamespacesDeployEnd(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameGamespacesDeployEnd,
                Data = state
            });
    }

    public async Task SendSyncStartGameStarted(SyncStartGameStartedState state)
    {
        await _hubContext
            .Clients
            .Users(GetGameUserIds(state.Game.Id))
            .SyncStartGameStarted(new GameHubEvent<SyncStartGameStartedState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.SyncStartGameStarted,
                Data = state
            });
    }

    public async Task SendSyncStartGameStarting(SyncStartState state)
    {
        await _hubContext
            .Clients
            .Users(GetGameUserIds(state.Game.Id))
            .SyncStartGameStarting(new GameHubEvent<SyncStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.SyncStartGameStarting,
                Data = state
            });
    }

    public async Task SendSyncStartGameStateChanged(SyncStartState state)
    {


        await _hubContext
            .Clients
            .Users(GetGameUserIds(state.Game.Id))
            .SyncStartGameStateChanged(new GameHubEvent<SyncStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.SyncStartGameStateChanged,
                Data = state
            });
    }

    // public async Task SendYourActiveGamesChanged(string userId)
    // {
    //     var enrollments = await GetActiveEnrollments(userId);

    //     await _hubContext
    //         .Clients
    //         .User(userId)
    //         .YourActiveGamesChanged(new GameHubEvent<YourActiveGamesChangedEvent>
    //         {
    //             GameId = string.Empty,
    //             EventType = GameHubEventType.YourActiveGamesChanged,
    //             Data = new YourActiveGamesChangedEvent
    //             {
    //                 UserId = userId,
    //                 ActiveEnrollments = enrollments
    //             }
    //         });
    // }

    public Task Handle(GameEnrolledPlayersChangeNotification notification, CancellationToken cancellationToken)
        => UpdateGameIdUserIdsMap(notification);

    private IEnumerable<string> GetGameUserIds(string gameId)
    {
        _gameIdUserIdsMap.TryGetValue(gameId, out var userIds);
        if (userIds.IsEmpty())
            return Array.Empty<string>();

        return userIds;
    }

    private async Task UpdateGameIdUserIdsMap(GameEnrolledPlayersChangeNotification notification)
    {
        var query = _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Game.GameEnd != DateTimeOffset.MinValue || p.Game.GameEnd > _now.Get())
            .Where(p => p.Game.PlayerMode == PlayerMode.Competition && p.Mode == PlayerMode.Competition)
            .Where(p => p.GameId == notification.Context.GameId)
            .Select(p => new
            {
                p.GameId,
                p.UserId
            });

        var theString = query.ToQueryString();

        var gameUserData = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Game.GameEnd != DateTimeOffset.MinValue || p.Game.GameEnd > _now.Get())
            .Where(p => p.Game.PlayerMode == PlayerMode.Competition && p.Mode == PlayerMode.Competition)
            .Where(p => p.GameId == notification.Context.GameId)
            .GroupBy(p => p.GameId)
            .Select(p => new
            {
                GameId = p.Key,
                UserIds = p.Select(x => x.UserId).Distinct()
            })
        .ToDictionaryAsync(gr => gr.GameId, gr => gr.UserIds);

        lock (_gameIdUserIdsMap)
        {
            _gameIdUserIdsMap.Clear();

            foreach (var key in gameUserData.Keys)
                _gameIdUserIdsMap.TryAdd(key, gameUserData[key].ToArray());
        }
    }
}
