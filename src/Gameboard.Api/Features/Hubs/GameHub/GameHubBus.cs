using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Gameboard.Api.Features.Games;

public interface IGameHubBus
{
    Task SendDeployChallengesStart(ExternalGameChallengeCreationState state);
    Task SendPlayerJoined(string playerConnectionId, PlayerJoinedEvent ev);
    // Task SendDeployChallengesPercentageChange(ExternalGameChallengeCreationState state);
    // Task SendDeployChallengesEnd(ExternalGameChallengeCreationState state);
    // DeployChallengesStart,
    // DeployChallengesPercentageChange,
    // DeployChallengesEnd,
    // Task SendCreatePlayerSessionsStart(GameSessionCreationState state);
    // Task SendCreatePlayerSessionsPercentageChange(GameSessionCreationState state);
    // Task SendCreatePlayerSessionsEnd(GameSessionCreationState state);
    Task SendSyncStartGameStateChanged(SyncStartState state);
    Task SendSyncStartGameStarting(SyncStartGameStartedState state);
}

internal class GameHubBus : IGameHubBus, IGameboardHubBus
{
    private readonly IHubContext<GameHub, IGameHubEvent> _hubContext;
    private readonly IMapper _mapper;

    public GameboardHubGroupType GroupType { get => GameboardHubGroupType.Game; }

    public GameHubBus
    (
        IHubContext<GameHub, IGameHubEvent> hubContext,
        IMapper mapper
    )
    {
        _hubContext = hubContext;
        _mapper = mapper;
    }

    public async Task SendDeployChallengesStart(ExternalGameChallengeCreationState state)
    {
        await _hubContext
            .SendToGroup(this, state.GameId)
            .ExternalGameChallengeDeployEvent(new GameHubEvent<ExternalGameChallengeCreationState>
            {
                GameId = state.GameId,
                EventType = GameHubEventType.DeployChallengesStart,
                Data = state
            });
    }

    public async Task SendPlayerJoined(string playerConnectionId, PlayerJoinedEvent ev)
    {
        await _hubContext
            // .SendToAllInGroupExcept(this, ev.GameId, playerConnectionId)
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
}
