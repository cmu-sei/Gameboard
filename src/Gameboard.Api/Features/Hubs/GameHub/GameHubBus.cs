using System.Threading.Tasks;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Gameboard.Api.Features.Games;

public interface IGameHubBus
{
    Task SendPlayerJoined(string playerConnectionId, PlayerJoinedEvent ev);
    Task SendYouJoined(string userId, YouJoinedEvent ev);
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
}

internal class GameHubBus : IGameHubBus, IGameboardHubBus
{
    private readonly IHubContext<GameHub, IGameHubEvent> _hubContext;

    public GameboardHubGroupType GroupType { get => GameboardHubGroupType.Game; }

    public GameHubBus(IHubContext<GameHub, IGameHubEvent> hubContext) => _hubContext = hubContext;

    public async Task SendExternalGameChallengesDeployStart(GameStartUpdate state)
    {
        await _hubContext
            .Clients
            .Group(this.GetCanonicalGroupId(state.Game.Id))
            .ExternalGameChallengesDeployStart(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameChallengesDeployStart,
                Data = state
            });
    }

    public Task SendExternalGameChallengesDeployProgressChange(GameStartUpdate state)
        => _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameChallengesDeployProgressChange(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameChallengesDeployProgressChange,
                Data = state
            });

    public Task SendExternalGameChallengesDeployEnd(GameStartUpdate state)
        => _hubContext
            .SendToGroup(this, state.Game.Id)
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
            .Group(this.GetCanonicalGroupId(state.Game.Id))
            .ExternalGameLaunchStart(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameLaunchStart,
                Data = state
            });
    }

    public Task SendExternalGameLaunchEnd(GameStartUpdate state)
        => _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameLaunchEnd(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameLaunchStart,
                Data = state
            });

    public Task SendExternalGameLaunchFailure(GameStartUpdate state)
        => _hubContext
            .SendToGroup(this, state.Game.Id)
            .ExternalGameLaunchFailure(new GameHubEvent<GameStartUpdate>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.ExternalGameLaunchFailure,
                Data = state
            });

    public async Task SendExternalGameGamespacesDeployStart(GameStartUpdate state)
    {
        await _hubContext
            .SendToGroup(this, state.Game.Id)
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
            .SendToGroup(this, state.Game.Id)
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
            .SendToGroup(this, state.Game.Id)
            .ExternalGameGamespacesDeployEnd(new GameHubEvent<GameStartUpdate>
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

    public async Task SendSyncStartGameStarted(SyncStartGameStartedState state)
    {
        await _hubContext
            .SendToGroup(this, state.Game.Id)
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
            .SendToGroup(this, state.Game.Id)
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
