using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Gameboard.Api.Features.Games;

public class GameHubEvent<TData> where TData : class
{
    public required string GameId { get; set; }
    public required TData Data { get; set; }
    public required GameHubEventType EventType { get; set; }
}

public enum GameHubEventType
{
    SyncStartStateChanged,
    SyncStartGameStarting
}

public interface IGameHubBus
{
    Task SendSyncStartStateChanged(SyncStartState state, User actor);
    Task SendSyncStartGameStarting(SynchronizedGameStartedState state);
}

internal class GameHubBus : IGameHubBus
{
    private readonly IHubContext<AppHub, IAppHubEvent> _hubContext;
    private readonly IMapper _mapper;

    public GameHubBus(IHubContext<AppHub, IAppHubEvent> hubContext, IMapper mapper)
    {
        _hubContext = hubContext;
        _mapper = mapper;
    }

    public async Task SendSyncStartStateChanged(SyncStartState state, User actor)
    {
        await _hubContext
            .Clients
            .Group(state.Game.Id)
            .GameHubEvent(new GameHubEvent<SyncStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.SyncStartStateChanged,
                Data = state
            });
    }

    public async Task SendSyncStartGameStarting(SynchronizedGameStartedState state)
    {
        await _hubContext
            .Clients
            .Group(state.Game.Id)
            .SynchronizedGameStartedEvent(new GameHubEvent<SynchronizedGameStartedState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.SyncStartGameStarting,
                Data = state
            });
    }
}
