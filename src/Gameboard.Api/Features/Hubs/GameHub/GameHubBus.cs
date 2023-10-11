using System.Threading.Tasks;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Gameboard.Api.Features.Games;

public interface IGameHubBus
{
    Task SendPlayerJoined(string playerConnectionId, PlayerJoinedEvent ev);
    Task SendYouJoined(string userId, YouJoinedEvent ev);
    Task SendExternalGameChallengesDeployStart(GameStartState state);
    Task SendExternalGameChallengesDeployProgressChange(GameStartState state);
    Task SendExternalGameChallengesDeployEnd(GameStartState state);
    Task SendExternalGameLaunchStart(GameStartState state);
    Task SendExternalGameLaunchEnd(GameStartState state);
    Task SendExternalGameLaunchFailure(GameStartState state);
    Task SendExternalGameGamespacesDeployStart(GameStartState state);
    Task SendExternalGameGamespacesDeployProgressChange(GameStartState state);
    Task SendExternalGameGamespacesDeployEnd(GameStartState state);
    Task SendSyncStartGameStateChanged(SyncStartState state);
    Task SendSyncStartGameStarting(SyncStartGameStartedState state);
}

internal class GameHubBus : IGameHubBus, IGameboardHubBus
{
    private readonly IHubContext<GameHub, IGameHubEvent> _hubContext;

    public GameboardHubGroupType GroupType { get => GameboardHubGroupType.Game; }

    public GameHubBus(IHubContext<GameHub, IGameHubEvent> hubContext) => _hubContext = hubContext;

    public async Task SendExternalGameChallengesDeployStart(GameStartState state)
    {
        await _hubContext
            .Clients
            .Group(this.GetCanonicalGroupId(state.Game.Id))
            .ExternalGameChallengesDeployStart(new GameHubEvent<GameStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameChallengesDeployStart,
                Data = state
            });
    }

    public Task SendExternalGameChallengesDeployProgressChange(GameStartState state)
        => _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameChallengesDeployProgressChange(new GameHubEvent<GameStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameChallengesDeployProgressChange,
                Data = state
            });

    public Task SendExternalGameChallengesDeployEnd(GameStartState state)
        => _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameChallengesDeployEnd(new GameHubEvent<GameStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameChallengesDeployEnd,
                Data = state
            });

    public Task SendExternalGameLaunchStart(GameStartState state)
    {
        return _hubContext
            .Clients
            .Group(this.GetCanonicalGroupId(state.Game.Id))
            .ExternalGameLaunchStart(new GameHubEvent<GameStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameLaunchStart,
                Data = state
            });
    }

    public Task SendExternalGameLaunchEnd(GameStartState state)
        => _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameLaunchEnd(new GameHubEvent<GameStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameLaunchStart,
                Data = state
            });

    public Task SendExternalGameLaunchFailure(GameStartState state)
        => _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameLaunchFailure(new GameHubEvent<GameStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameLaunchFailure,
                Data = state
            });

    public async Task SendExternalGameGamespacesDeployStart(GameStartState state)
    {
        await _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameGamespacesDeployStart(new GameHubEvent<GameStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameGamespacesDeployStart,
                Data = state
            });
    }

    public async Task SendExternalGameGamespacesDeployProgressChange(GameStartState state)
    {
        await _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameGamespacesDeployProgressChange(new GameHubEvent<GameStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameGamespacesDeployProgressChange,
                Data = state
            });
    }

    public async Task SendExternalGameGamespacesDeployEnd(GameStartState state)
    {
        await _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameGamespacesDeployEnd(new GameHubEvent<GameStartState>
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
                GameId = model.GameId,
                EventType = GameHubEventType.YouJoined,
                Data = model
            });
}
