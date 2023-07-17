using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Gameboard.Api.Features.Games;

/// <summary>
/// This is separate from AppHub because it encapsulates hub management functionality that we want to be available server side
/// but not client side - every public method on AppHub is available to SignalR clients.
/// </summary>
public interface IInternalHubBus
{
    // Task Leave(Api.Player p, User actor);
    Task SendPlayerEnrolled(Api.Player p, User actor);
    Task SendPlayerLeft(Api.Player p, User actor);
    Task SendTeamDeleted(TeamState teamState, SimpleEntity actor);
    Task SendPlayerRoleChanged(Api.Player p, User actor);
    Task SendTeamSessionStarted(Api.Player p, User actor);
    Task SendTeamUpdated(Api.Player p, User actor);
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

    public async Task SendPlayerLeft(Api.Player p, User actor)
    {
        await _hubContext.Clients
            .Group(p.TeamId)
            .PlayerEvent(
                new HubEvent<TeamPlayer>
                {
                    Action = EventAction.Departed,
                    Model = _mapper.Map<TeamPlayer>(p),
                    ActingUser = new SimpleEntity { Id = actor.Id, Name = actor.ApprovedName }
                }
            );
    }

    public async Task SendTeamDeleted(TeamState teamState, SimpleEntity actor)
    {
        await _hubContext.Clients.Group(teamState.Id).TeamEvent(new HubEvent<TeamState>
        {
            Model = teamState,
            Action = EventAction.Deleted,
            ActingUser = actor
        });
    }

    public async Task SendPlayerEnrolled(Api.Player p, User actor)
    {
        var mappedPlayer = _mapper.Map<TeamPlayer>(p);

        await _hubContext
            .Clients
            .Group(p.TeamId)
            .PlayerEvent(new HubEvent<TeamPlayer>
            {
                Model = mappedPlayer,
                Action = EventAction.Created,
                ActingUser = new SimpleEntity { Id = actor.Id, Name = actor.ApprovedName }
            });
    }

    public async Task SendPlayerRoleChanged(Api.Player p, User actor)
    {
        var mappedPlayer = _mapper.Map<TeamPlayer>(p);

        await _hubContext.Clients
            .Group(p.TeamId)
            .PlayerEvent(new HubEvent<TeamPlayer>
            {
                Model = mappedPlayer,
                Action = EventAction.RoleChanged,
                ActingUser = new SimpleEntity { Id = actor.Id, Name = actor.ApprovedName }
            });
    }

    public async Task SendTeamSessionStarted(Api.Player p, User actor)
    {
        var teamState = new TeamState
        {
            Id = p.TeamId,
            Name = p.ApprovedName,
            SessionBegin = p.SessionBegin.IsEmpty() ? null : p.SessionBegin,
            SessionEnd = p.SessionEnd.IsEmpty() ? null : p.SessionEnd,
            Actor = actor.ToSimpleEntity()
        };

        await _hubContext.Clients
            .Group(p.TeamId)
            .TeamEvent(new HubEvent<TeamState>
            {
                Model = teamState,
                Action = EventAction.Started,
                ActingUser = new SimpleEntity { Id = actor.Id, Name = actor.ApprovedName }
            });
    }

    public async Task SendTeamUpdated(Api.Player p, User actor)
    {
        var teamState = new TeamState
        {
            Id = p.TeamId,
            Name = p.ApprovedName,
            SessionBegin = p.SessionBegin.IsEmpty() ? null : p.SessionBegin,
            SessionEnd = p.SessionEnd.IsEmpty() ? null : p.SessionEnd,
            Actor = new SimpleEntity { Id = actor.Id, Name = actor.ApprovedName }
        };

        await _hubContext.Clients
            .Group(p.TeamId)
            .TeamEvent(new HubEvent<TeamState>
            {
                Model = teamState,
                Action = EventAction.Updated,
                ActingUser = new SimpleEntity { Id = actor.Id, Name = actor.ApprovedName }
            });
    }
}
