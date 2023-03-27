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
    PlayerReadyStateChanged
}

public interface IGameHubBus
{
    Task SendPlayerReadyStateChanged(SyncStartState state, User actor);
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

    public async Task SendPlayerReadyStateChanged(SyncStartState state, User actor)
    {
        await _hubContext
            .Clients
            .Group(state.Game.Id)
            .SyncStartEvent(new GameHubEvent<SyncStartState>
            {
                GameId = state.Game.Id,
                EventType = GameHubEventType.PlayerReadyStateChanged,
                Data = state
            });
    }
}
