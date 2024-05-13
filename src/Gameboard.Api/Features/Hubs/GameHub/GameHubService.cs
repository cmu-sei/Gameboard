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

public interface IGameHubService : INotificationHandler<GameEnrolledPlayersChangeNotification>, INotificationHandler<AppStartupNotification>, INotificationHandler<GameCacheInvalidateNotification>
{
    // invoke functions on clients
    Task SendChallengesDeployStart(GameHubEvent ev);
    Task SendChallengesDeployProgressChange(GameHubEvent ev);
    Task SendChallengesDeployEnd(GameHubEvent ev);
    Task SendLaunchStart(GameHubEvent ev);
    Task SendLaunchEnd(GameHubEvent ev);
    Task SendLaunchFailure(GameHubEvent ev);
    Task SendGamespacesDeployStart(GameHubEvent ev);
    Task SendGamespacesDeployProgressChange(GameHubEvent ev);
    Task SendGamespacesDeployEnd(GameHubEvent ev);
    Task SendSyncStartGameStateChanged(SyncStartState state);
    Task SendSyncStartGameStarted(SyncStartGameStartedState state);
    Task SendSyncStartGameStarting(SyncStartState state);
}

internal class GameHubService : IGameHubService, IGameboardHubService
{
    private static readonly ConcurrentDictionary<string, string[]> _gameIdUserIdsMap = new();
    private static readonly ConcurrentDictionary<string, string[]> _teamIdUserIdsMap = new();

    private readonly IHubContext<GameHub, IGameHubEvent> _hubContext;
    private readonly INowService _now;
    private readonly IGameResourcesDeployStatusService _resourcesDeployStatus;
    private readonly IStore _store;

    public GameboardHubType GroupType => GameboardHubType.Game;

    public GameHubService
    (
        IHubContext<GameHub, IGameHubEvent> hubContext,
        INowService now,
        IGameResourcesDeployStatusService resourcesDeployStatus,
        IStore store
    )
    {
        _hubContext = hubContext;
        _now = now;
        _resourcesDeployStatus = resourcesDeployStatus;
        _store = store;
    }

    public async Task SendChallengesDeployStart(GameHubEvent ev)
    {
        await _hubContext
            .Clients
            .Users(GetTeamsUserIds(ev.TeamIds))
            .ChallengesDeployStart(new GameHubEvent<GameResourcesDeployStatus>()
            {
                GameId = ev.GameId,
                TeamIds = ev.TeamIds,
                Data = await _resourcesDeployStatus.GetStatus(ev.GameId)
            });
    }

    public async Task SendChallengesDeployProgressChange(GameHubEvent ev)
        => await _hubContext
            .Clients
            .Users(GetTeamsUserIds(ev.TeamIds))
            .ChallengesDeployProgressChange(new GameHubEvent<GameResourcesDeployStatus>
            {
                GameId = ev.GameId,
                TeamIds = ev.TeamIds,
                Data = await _resourcesDeployStatus.GetStatus(ev.GameId)
            });

    public async Task SendChallengesDeployEnd(GameHubEvent ev)
        => await _hubContext
            .Clients
            .Users(GetTeamsUserIds(ev.TeamIds))
            .ChallengesDeployEnd(new GameHubEvent<GameResourcesDeployStatus>
            {
                GameId = ev.GameId,
                TeamIds = ev.TeamIds,
                Data = await _resourcesDeployStatus.GetStatus(ev.GameId)
            });

    public async Task SendLaunchStart(GameHubEvent ev)
        => await _hubContext
            .Clients
            .Users(GetTeamsUserIds(ev.TeamIds))
            .LaunchStart(new GameHubEvent<GameResourcesDeployStatus>
            {
                GameId = ev.GameId,
                TeamIds = ev.TeamIds,
                Data = await _resourcesDeployStatus.GetStatus(ev.GameId)
            });

    public async Task SendLaunchEnd(GameHubEvent ev)
        => await _hubContext
            .Clients
            .Users(GetTeamsUserIds(ev.TeamIds))
            .LaunchEnd(new GameHubEvent<GameResourcesDeployStatus>
            {
                GameId = ev.GameId,
                TeamIds = ev.TeamIds,
                Data = await _resourcesDeployStatus.GetStatus(ev.GameId)
            });

    public async Task SendLaunchFailure(GameHubEvent ev)
        => await _hubContext
            .Clients
            .Users(GetTeamsUserIds(ev.TeamIds))
            .LaunchFailure(new GameHubEvent<GameResourcesDeployStatus>
            {
                GameId = ev.GameId,
                TeamIds = ev.TeamIds,
                Data = await _resourcesDeployStatus.GetStatus(ev.GameId)
            });

    public async Task SendGamespacesDeployStart(GameHubEvent ev)
    {
        await _hubContext
            .Clients
            .Users(GetTeamsUserIds(ev.TeamIds))
            .GamespacesDeployStart(new GameHubEvent<GameResourcesDeployStatus>
            {
                GameId = ev.GameId,
                TeamIds = ev.TeamIds,
                Data = await _resourcesDeployStatus.GetStatus(ev.GameId)
            });
    }

    public async Task SendGamespacesDeployProgressChange(GameHubEvent ev)
    {
        await _hubContext
            .Clients
            .Users(GetTeamsUserIds(ev.TeamIds))
            .GamespacesDeployProgressChange(new GameHubEvent<GameResourcesDeployStatus>
            {
                GameId = ev.GameId,
                TeamIds = ev.TeamIds,
                Data = await _resourcesDeployStatus.GetStatus(ev.GameId)
            });
    }

    public async Task SendGamespacesDeployEnd(GameHubEvent ev)
    {
        await _hubContext
            .Clients
            .Users(GetTeamsUserIds(ev.TeamIds))
            .GamespacesDeployEnd(new GameHubEvent<GameResourcesDeployStatus>
            {
                GameId = ev.GameId,
                TeamIds = ev.TeamIds,
                Data = await _resourcesDeployStatus.GetStatus(ev.GameId)
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
                TeamIds = state.Teams.Keys,
                Data = state
            });
    }

    public async Task SendSyncStartGameStarting(SyncStartState state)
    {
        var teamIds = state.Teams.Select(t => t.Id);

        await _hubContext
            .Clients
            .Users(GetTeamsUserIds(teamIds))
            .SyncStartGameStarting(new GameHubEvent<SyncStartState>
            {
                GameId = state.Game.Id,
                TeamIds = teamIds,
                Data = state
            });
    }

    public async Task SendSyncStartGameStateChanged(SyncStartState state)
    {
        var teamIds = state.Teams.Select(t => t.Id);

        await _hubContext
            .Clients
            .Users(GetTeamsUserIds(teamIds))
            .SyncStartGameStateChanged(new GameHubEvent<SyncStartState>
            {
                GameId = state.Game.Id,
                TeamIds = teamIds,
                Data = state
            });
    }

    public async Task Handle(AppStartupNotification appStartupNotification, CancellationToken cancellationToken)
    {
        // build the game/user association cache for all sync start or external games proactively
        var games = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.RequireSynchronizedStart || g.Mode == GameEngineMode.External)
            .Select(g => new { g.Id, g.RequireSynchronizedStart })
            .ToArrayAsync(cancellationToken);

        foreach (var game in games)
            await UpdateGameAndTeamUserIdsMaps(game.Id);
    }

    public Task Handle(GameCacheInvalidateNotification notification, CancellationToken cancellationToken)
        => UpdateGameAndTeamUserIdsMaps(notification.GameId);

    public Task Handle(GameEnrolledPlayersChangeNotification notification, CancellationToken cancellationToken)
        => UpdateGameAndTeamUserIdsMaps(notification.Context.GameId);

    private IEnumerable<string> GetGameUserIds(string gameId)
    {
        _gameIdUserIdsMap.TryGetValue(gameId, out var userIds);
        if (userIds.IsEmpty())
            return Array.Empty<string>();

        return userIds;
    }

    private IEnumerable<string> GetTeamsUserIds(IEnumerable<string> teamIds)
    {
        var userIds = new List<string>();

        foreach (var teamId in teamIds)
        {
            var teamUserIds = _teamIdUserIdsMap.TryGetValue(teamId, out var results) ? results : Array.Empty<string>();
            userIds.AddRange(teamUserIds);
        }

        return userIds.Distinct().ToArray();
    }


    private async Task UpdateGameAndTeamUserIdsMaps(string gameId)
    {
        var gameAndTeamUsers = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.Game.GameEnd != DateTimeOffset.MinValue || p.Game.GameEnd > _now.Get())
            .Where(p => p.Game.PlayerMode == PlayerMode.Competition && p.Mode == PlayerMode.Competition)
            .Where(p => p.GameId == gameId)
            .Select(p => new { p.TeamId, p.UserId })
            .Distinct()
            .ToArrayAsync();

        lock (_gameIdUserIdsMap)
        {
            _gameIdUserIdsMap.TryRemove(gameId, out var existingValue);
            _gameIdUserIdsMap.TryAdd(gameId, gameAndTeamUsers.Select(gu => gu.UserId).ToArray());
        }

        var teamUsers = gameAndTeamUsers.GroupBy(g => g.TeamId).ToDictionary(u => u.Key, u => u.Select(thing => thing.UserId).ToArray());
        lock (_teamIdUserIdsMap)
        {
            foreach (var entry in teamUsers)
            {
                _teamIdUserIdsMap.TryRemove(entry.Key, out var existingValue);
                _teamIdUserIdsMap.TryAdd(entry.Key, entry.Value);
            }
        }
    }
}