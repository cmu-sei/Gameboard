// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers;

[Authorize]
public class TicketController
(
    IActingUserService actingUserService,
    ILogger<ChallengeController> logger,
    IDistributedCache cache,
    TicketValidator validator,
    CoreOptions options,
    IUserRolePermissionsService permissionsService,
    TicketService ticketService,
    IHubContext<AppHub, IAppHubEvent> hub,
    IMapper mapper
) : GameboardLegacyController(actingUserService, logger, cache, validator)
{
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    TicketService TicketService { get; } = ticketService;
    public CoreOptions Options { get; } = options;
    IHubContext<AppHub, IAppHubEvent> Hub { get; } = hub;
    IMapper Mapper { get; } = mapper;

    /// <summary>
    /// Gets ticket details
    /// </summary>
    /// <param name="id"></param>
    /// <param name="sortDirection">The direction in which activity on this ticket will be ordered (by timestamp)</param>
    /// <returns></returns>
    [HttpGet("api/ticket/{id}")]
    [Authorize]
    public async Task<Ticket> Retrieve([FromRoute] int id, [FromQuery] SortDirection sortDirection)
    {
        await AuthorizeAny
        (
            () => _permissionsService.Can(PermissionKey.Support_ViewTickets),
            () => TicketService.IsOwnerOrTeamMember(id, Actor.Id)
        );

        return await TicketService.Retrieve(id, sortDirection);
    }


    /// <summary>
    /// Create new ticket
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("/api/ticket")]
    [Authorize]
    public async Task<Ticket> Create([FromForm] NewTicket model)
    {
        var result = await TicketService.Create(model);
        await Notify(Mapper.Map<TicketNotification>(result), EventAction.Created);
        return result;
    }

    /// <summary>
    /// Update ticket
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPut("/api/ticket")]
    [Authorize]
    public async Task<Ticket> Update([FromBody] ChangedTicket model)
    {
        var isTicketAdmin = await _permissionsService.Can(PermissionKey.Support_ManageTickets);
        if (!isTicketAdmin)
            await Authorize(TicketService.UserCanUpdate(model.Id, Actor.Id));

        await Validate(model);

        // Retrieve the previous ticket result for comparison soon
        var prevTicket = await TicketService.Retrieve(model.Id);

        var result = await TicketService.Update(model, Actor.Id, isTicketAdmin);
        if (result.Label != prevTicket.Label) prevTicket.LastUpdated = result.LastUpdated;

        // If the ticket hasn't been meaningfully updated, don't send a notification
        if (prevTicket.LastUpdated != result.LastUpdated)
        {
            await Notify(Mapper.Map<TicketNotification>(result), EventAction.Updated);
        }

        return result;
    }

    /// <summary>
    /// Lists tickets based on search params
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet("/api/ticket/list")]
    [Authorize]
    public async Task<IEnumerable<TicketSummary>> List([FromQuery] TicketSearchFilter model)
        => await TicketService.List(model, Actor.Id, await _permissionsService.Can(PermissionKey.Support_ViewTickets));

    /// <summary>
    /// Create new ticket comment
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("/api/ticket/comment")]
    [Authorize]
    public async Task<TicketActivity> AddComment([FromForm] NewTicketComment model)
    {
        await AuthorizeAny
        (
            () => _permissionsService.Can(PermissionKey.Support_ManageTickets),
            () => TicketService.IsOwnerOrTeamMember(model.TicketId, Actor.Id)
        );

        await Validate(model);
        var result = await TicketService.AddComment(model, Actor.Id);
        await Notify(Mapper.Map<TicketNotification>(result), EventAction.Updated);

        return result;
    }


    /// <summary>
    /// Lists all distinct labels
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet("/api/ticket/labels")]
    [Authorize]
    public async Task<string[]> ListLabels([FromQuery] SearchFilter model)
    {
        await Authorize(_permissionsService.Can(PermissionKey.Support_ViewTickets));
        return await TicketService.ListLabels(model);
    }

    private Task Notify(TicketNotification notification, EventAction action)
    {
        var ev = new HubEvent<TicketNotification>
        {
            Model = notification,
            Action = action,
            ActingUser = Actor.ToSimpleEntity()
        };

        var tasks = new List<Task>
        {
            Hub.Clients.Group(AppConstants.InternalSupportChannel).TicketEvent(ev)
        };

        if (!string.IsNullOrEmpty(notification.TeamId))
            tasks.Add(Hub.Clients.Group(notification.TeamId).TicketEvent(ev));
        else
            tasks.Add(Hub.Clients.Group(notification.RequesterId).TicketEvent(ev));

        return Task.WhenAll(tasks);
    }
}
