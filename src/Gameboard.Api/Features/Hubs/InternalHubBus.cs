using System;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

public interface IInternalHubBus
{
    Task SendPlayerLeft(Player p, User actor);
    Task SendTeamDeleted(Player p, User actor);
    Task SendTeamStarted(Player p, User actor);
    Task SendTeamUpdated(Player p, User actor);
}

internal class InternalHubBus : IInternalHubBus
{
    private IHubContext<AppHub, IAppHubEvent> _hub;
    private readonly IMapper _mapper;

    public InternalHubBus(IHubContext<AppHub, IAppHubEvent> hub, IMapper mapper)
    {
        _hub = hub;
        _mapper = mapper;
    }

    public async Task SendPlayerLeft(Player p, User actor)
    {
        await this._hub.Clients
            .Group(p.TeamId)
            .PresenceEvent(
                new HubEvent<TeamPlayer>(_mapper.Map<TeamPlayer>(p), EventAction.Departed, actor.Id)
            );
    }

    public async Task SendTeamDeleted(Player p, User actor)
    {
        var teamState = _mapper.Map<TeamState>(p, opts => opts.AfterMap((src, dest) =>
        {
            dest.Actor = actor;
        }));

        await this._hub.Clients.Group(p.TeamId).TeamEvent(new HubEvent<TeamState>(teamState, EventAction.Deleted, actor.Id));
    }

    public async Task SendTeamStarted(Player p, User actor)
    {
        var teamState = _mapper.Map<TeamState>(p, opts => opts.AfterMap((src, dest) =>
        {
            dest.Actor = actor;
        }));

        await this._hub.Clients
            .Group(p.TeamId)
            .TeamEvent(
                new HubEvent<TeamState>(teamState, EventAction.Started, actor.Id)
            );
    }

    public async Task SendTeamUpdated(Player p, User actor)
    {
        var teamState = _mapper.Map<TeamState>(p, opts => opts.AfterMap((src, dest) =>
        {
            dest.Actor = actor;
        }));

        await this._hub.Clients
            .Group(p.TeamId)
            .TeamEvent(
                new HubEvent<TeamState>(teamState, EventAction.Updated, actor.Id)
            );
    }
}
