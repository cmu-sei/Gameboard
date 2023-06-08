using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Features.Games;

public interface IGameHubBus
{
    Task SendPlayerJoined(string playerConnectionId, PlayerJoinedEvent ev);
    Task SendYouJoined(string userId, YouJoinedEvent ev);
    Task SendExternalGameChallengesDeployStart(ExternalGameLaunchState state);
    Task SendExternalGameChallengesDeployProgressChange(ExternalGameLaunchState state);
    Task SendExternalGameChallengesDeployEnd(ExternalGameLaunchState state);
    Task SendExternalGameLaunchStart(ExternalGameLaunchState state);
    Task SendExternalGameLaunchEnd(ExternalGameLaunchState state);
    Task SendExternalGameLaunchFailure(ExternalGameLaunchState state);
    Task SendExternalGameGamespacesDeployStart(ExternalGameLaunchState state);
    Task SendExternalGameGamespacesDeployProgressChange(ExternalGameLaunchState state);
    Task SendExternalGameGamespacesDeployEnd(ExternalGameLaunchState state);
    Task SendSyncStartGameStateChanged(SyncStartState state);
    Task SendSyncStartGameStarting(SyncStartGameStartedState state);
}

internal class GameHubBus : IGameHubBus, IGameboardHubBus
{
    private readonly IMemoryCache _cache;
    private readonly IHubContext<GameHub, IGameHubEvent> _hubContext;
    private readonly IMapper _mapper;

    public GameboardHubGroupType GroupType { get => GameboardHubGroupType.Game; }

    public GameHubBus
    (
        IMemoryCache cache,
        IHubContext<GameHub, IGameHubEvent> hubContext,
        IMapper mapper
    )
    {
        _cache = cache;
        _hubContext = hubContext;
        _mapper = mapper;
    }

    public async Task SendExternalGameChallengesDeployStart(ExternalGameLaunchState state)
    {
        IList<string> userIds;
        _cache.TryGetValue(state.Game.Id, out userIds);

        await _hubContext
            .Clients
            .Group(this.GetCanonicalGroupId(state.Game.Id))
            .ExternalGameChallengesDeployStart(new GameHubEvent<ExternalGameLaunchState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameChallengesDeployStart,
                Data = state
            });
    }

    public Task SendExternalGameChallengesDeployProgressChange(ExternalGameLaunchState state)
        => _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameChallengesDeployProgressChange(new GameHubEvent<ExternalGameLaunchState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameChallengesDeployProgressChange,
                Data = state
            });

    public Task SendExternalGameChallengesDeployEnd(ExternalGameLaunchState state)
        => _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameChallengesDeployEnd(new GameHubEvent<ExternalGameLaunchState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameChallengesDeployEnd,
                Data = state
            });

    public Task SendExternalGameLaunchStart(ExternalGameLaunchState state)
    {
        var canoncalGroupId = this.GetCanonicalGroupId(state.Game.Id);

        return _hubContext
            .Clients
            .Group(this.GetCanonicalGroupId(state.Game.Id))
            .ExternalGameLaunchStart(new GameHubEvent<ExternalGameLaunchState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameLaunchStart,
                Data = state
            });
    }

    public Task SendExternalGameLaunchEnd(ExternalGameLaunchState state)
        => _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameLaunchEnd(new GameHubEvent<ExternalGameLaunchState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameLaunchStart,
                Data = state
            });

    public Task SendExternalGameLaunchFailure(ExternalGameLaunchState state)
        => _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameLaunchFailure(new GameHubEvent<ExternalGameLaunchState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameLaunchFailure,
                Data = state
            });

    public async Task SendExternalGameGamespacesDeployStart(ExternalGameLaunchState state)
    {
        await _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameGamespacesDeployStart(new GameHubEvent<ExternalGameLaunchState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameGamespacesDeployStart,
                Data = state
            });
    }

    public async Task SendExternalGameGamespacesDeployProgressChange(ExternalGameLaunchState state)
    {
        await _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameGamespacesDeployProgressChange(new GameHubEvent<ExternalGameLaunchState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameGamespacesDeployProgressChange,
                Data = state
            });
    }

    public async Task SendExternalGameGamespacesDeployEnd(ExternalGameLaunchState state)
    {
        await _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameGamespacesDeployEnd(new GameHubEvent<ExternalGameLaunchState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameGamespacesDeployEnd,
                Data = state
            });
    }

    public async Task SendPlayerJoined(string playerConnectionId, PlayerJoinedEvent ev)
    {
        await _hubContext
            .SendToGroup(this, ev.GameId)
            .PlayerJoined(new GameHubEvent<PlayerJoinedEvent>
            {
                GameId = ev.GameId,
                EventType = GameHubEventType.PlayerJoined,
                Data = ev
            });
    }

    public async Task SendSyncStartGameStarting(SyncStartGameStartedState state)
    {
        await _hubContext
            .SendToGroup(this, state.Game.Id)
            .SyncStartGameStarting(new GameHubEvent<SyncStartGameStartedState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.SyncStartGameStarting,
                Data = state
            });
    }

    public async Task SendSyncStartGameStateChanged(SyncStartState state)
    {
        await _hubContext
            .SendToGroup(this, state.Game.Id)
            .SyncStartGameStateChanged(new GameHubEvent<SyncStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.SyncStartGameStateChanged,
                Data = state
            });
    }

    public Task SendYouJoined(string userId, YouJoinedEvent model)
        => _hubContext
            .Clients
            .User(userId)
            .YouJoined(new GameHubEvent<YouJoinedEvent>
            {
                GameId = null,
                EventType = GameHubEventType.YouJoined,
                Data = model
            });
}
