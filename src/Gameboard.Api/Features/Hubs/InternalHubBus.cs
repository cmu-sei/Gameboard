using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Features.Teams;
using Microsoft.AspNetCore.SignalR;

namespace Gameboard.Api.Hubs;

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
    private readonly IHubContext<AppHub, IAppHubEvent> _hubContext;
    private readonly IMapper _mapper;

    public InternalHubBus(IHubContext<AppHub, IAppHubEvent> hubContext, IMapper mapper)
    {
        _hubContext = hubContext;
        _mapper = mapper;
    }

    public async Task SendPlayerLeft(Player p, User actor)
    {
        await this._hubContext.Clients
            .Group(p.TeamId)
            .PlayerEvent(
                new HubEvent<TeamPlayer>
                {
                    Action = EventAction.Departed,
                    Model = _mapper.Map<TeamPlayer>(p),
                    ActingUser = BuildUserDescription(actor)
                }
            );
    }

    public async Task SendTeamDeleted(Player p, User actor)
    {
        var teamState = _mapper.Map<TeamState>(p, opts => opts.AfterMap((src, dest) =>
        {
            dest.Actor = actor;
        }));

        await _hubContext.Clients.Group(p.TeamId).TeamEvent(new HubEvent<TeamState>
        {
            Model = teamState,
            Action = EventAction.Deleted,
            ActingUser = BuildUserDescription(actor)
        });
    }

    public async Task SendPlayerEnrolled(Player p, User actor)
    {
        var mappedPlayer = _mapper.Map<TeamPlayer>(p);

        await _hubContext
            .Clients
            .Group(p.TeamId)
            .PlayerEvent(new HubEvent<TeamPlayer>
            {
                Model = mappedPlayer,
                Action = EventAction.Created,
                ActingUser = BuildUserDescription(actor)
            });
    }

    public async Task SendPlayerRoleChanged(Player p, User actor)
    {
        var mappedPlayer = _mapper.Map<TeamPlayer>(p);

        await _hubContext.Clients
            .Group(p.TeamId)
            .PlayerEvent(new HubEvent<TeamPlayer>
            {
                Model = mappedPlayer,
                Action = EventAction.RoleChanged,
                ActingUser = BuildUserDescription(actor)
            });
    }

    public async Task SendTeamSessionStarted(Player p, User actor)
    {
        var teamState = _mapper.Map<TeamState>(p, opts => opts.AfterMap((src, dest) =>
        {
            dest.Actor = actor;
        }));

        await _hubContext.Clients
            .Group(p.TeamId)
            .TeamEvent(new HubEvent<TeamState>
            {
                Model = teamState,
                Action = EventAction.Started,
                ActingUser = BuildUserDescription(actor)
            });
    }

    public async Task SendTeamUpdated(Player p, User actor)
    {
        var teamState = _mapper.Map<TeamState>(p, opts => opts.AfterMap((src, dest) =>
        {
            dest.Actor = actor;
        }));

        await _hubContext.Clients
            .Group(p.TeamId)
            .TeamEvent(new HubEvent<TeamState>
            {
                Model = teamState,
                Action = EventAction.Updated,
                ActingUser = BuildUserDescription(actor)
            });
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
