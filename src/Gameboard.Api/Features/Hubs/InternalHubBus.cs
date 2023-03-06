using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

/// <summary>
/// This is separate from AppHub because it encapsulates hub management functionality that we want to be available server side
/// but not client side - every public method on AppHub is available to clients.
/// </summary>
public interface IInternalHubBus
{
    // Task Leave(Player p, User actor);
    Task SendPlayerEnrolled(Player p, User actor);
    Task SendPlayerLeft(Player p, User actor);
    Task SendTeamDeleted(Player p, User actor);
    Task SendPlayerRoleChanged(Player p, User actor);
    Task SendTeamSessionStarted(Player p, User actor);
    Task SendTeamUpdated(Player p, User actor);
}

internal class InternalHubBus : IInternalHubBus
{
    private readonly Hub<IAppHubEvent> _hub;
    private readonly IHubContext<AppHub, IAppHubEvent> _hubContext;
    private readonly IMapper _mapper;

    public InternalHubBus(Hub<IAppHubEvent> hub, IHubContext<AppHub, IAppHubEvent> hubContext, IMapper mapper)
    {
        _hub = hub;
        _hubContext = hubContext;
        _mapper = mapper;
    }

    public async Task SendPlayerLeft(Player p, User actor)
    {
        await this._hubContext.Clients
            .Group(p.TeamId)
            .PlayerEvent(
                new HubEvent<TeamPlayer>(_mapper.Map<TeamPlayer>(p), EventAction.Departed, BuildUserDescription(actor))
            );
    }

    public async Task SendTeamDeleted(Player p, User actor)
    {
        var teamState = _mapper.Map<TeamState>(p, opts => opts.AfterMap((src, dest) =>
        {
            dest.Actor = actor;
        }));

        await _hubContext.Clients.Group(p.TeamId).TeamEvent(new HubEvent<TeamState>(teamState, EventAction.Deleted, BuildUserDescription(actor)));
    }

    public async Task SendPlayerEnrolled(Player p, User actor)
    {
        var mappedPlayer = _mapper.Map<TeamPlayer>(p);

        await _hubContext
            .Clients
            .Group(p.TeamId)
            .PlayerEvent(
                new HubEvent<TeamPlayer>(mappedPlayer, EventAction.Created, BuildUserDescription(actor))
            );
    }

    public async Task SendPlayerRoleChanged(Player p, User actor)
    {
        var mappedPlayer = _mapper.Map<TeamPlayer>(p);

        await _hubContext.Clients
            .Group(p.TeamId)
            .PlayerEvent(new HubEvent<TeamPlayer>(mappedPlayer, EventAction.RoleChanged, BuildUserDescription(actor)));
    }

    public async Task SendTeamSessionStarted(Player p, User actor)
    {
        var teamState = _mapper.Map<TeamState>(p, opts => opts.AfterMap((src, dest) =>
        {
            dest.Actor = actor;
        }));

        await _hubContext.Clients
            .Group(p.TeamId)
            .TeamEvent(
                new HubEvent<TeamState>(teamState, EventAction.Started, BuildUserDescription(actor))
            );
    }

    public async Task SendTeamUpdated(Player p, User actor)
    {
        var teamState = _mapper.Map<TeamState>(p, opts => opts.AfterMap((src, dest) =>
        {
            dest.Actor = actor;
        }));

        await _hubContext.Clients
            .Group(p.TeamId)
            .TeamEvent(
                new HubEvent<TeamState>(teamState, EventAction.Updated, BuildUserDescription(actor))
            );
    }

    private HubEventActingUserDescription BuildUserDescription(User user)
    {
        return new HubEventActingUserDescription
        {
            Id = user.Id,
            Name = user.Name
        };
    }
}
