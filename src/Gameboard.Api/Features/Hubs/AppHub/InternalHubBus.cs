// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Features.Teams;
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
    Task SendTeamSessionReset(TeamHubSessionResetEvent resetEvent);
    Task SendTeamSessionStarted(StartTeamSessionsResultTeam team, string gameId, User actor);
    Task SendTeamSessionStarted(Api.Player player, User actor);
    Task SendTeamSessionExtended(TeamState teamState, User actor);
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

    public async Task SendTeamSessionExtended(TeamState teamState, User actor)
        => await _hubContext
            .Clients
            .Group(teamState.Id)
            .TeamEvent(new HubEvent<TeamState>
            {
                Action = EventAction.SessionExtended,
                Model = teamState,
                ActingUser = new SimpleEntity { Id = actor.Id, Name = actor.ApprovedName }
            });

    public async Task SendTeamSessionStarted(Api.Player p, User actor)
    {
        var teamState = new TeamState
        {
            Id = p.TeamId,
            ApprovedName = p.ApprovedName,
            Name = p.Name,
            NameStatus = p.NameStatus,
            GameId = p.GameId,
            SessionBegin = p.SessionBegin.IsEmpty() ? null : p.SessionBegin,
            SessionEnd = p.SessionEnd.IsEmpty() ? null : p.SessionEnd,
            Actor = actor.ToSimpleEntity(),
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

    public async Task SendTeamSessionStarted(StartTeamSessionsResultTeam team, string gameId, User actor)
    {
        var teamState = new TeamState
        {
            Id = team.Id,
            ApprovedName = team.Name,
            Name = team.Name,
            NameStatus = "approved",
            GameId = gameId,
            SessionBegin = team.SessionWindow.Start,
            SessionEnd = team.SessionWindow.End,
            Actor = actor.ToSimpleEntity(),
        };

        await _hubContext.Clients
            .Group(team.Id)
            .TeamEvent(new HubEvent<TeamState>
            {
                Model = teamState,
                Action = EventAction.Started,
                ActingUser = new SimpleEntity { Id = actor.Id, Name = actor.ApprovedName }
            });
    }

    public async Task SendTeamSessionReset(TeamHubSessionResetEvent resetEvent)
    {
        await _hubContext.Clients
            .Group(resetEvent.Id)
            .TeamEvent(new HubEvent<TeamState>
            {
                Model = new TeamState
                {
                    Id = resetEvent.Id,
                    ApprovedName = "",
                    Name = "",
                    NameStatus = "",
                    GameId = resetEvent.GameId,
                    SessionBegin = null,
                    SessionEnd = null,
                    Actor = resetEvent.ActingUser
                },
                Action = EventAction.SessionReset,
                ActingUser = resetEvent.ActingUser
            });
    }

    public async Task SendTeamUpdated(Api.Player p, User actor)
    {
        var teamState = new TeamState
        {
            Id = p.TeamId,
            ApprovedName = p.ApprovedName,
            GameId = p.GameId,
            Name = p.Name,
            NameStatus = p.NameStatus,
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
