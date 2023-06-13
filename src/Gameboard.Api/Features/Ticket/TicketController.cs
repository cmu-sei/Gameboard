// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common;
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
public class TicketController : _Controller
{
    private readonly IFileUploadService _fileUploadService;
    TicketService TicketService { get; }
    public CoreOptions Options { get; }
    IHubContext<AppHub, IAppHubEvent> Hub { get; }
    IMapper Mapper { get; }

    public TicketController(
        ILogger<ChallengeController> logger,
        IDistributedCache cache,
        TicketValidator validator,
        IFileUploadService fileUploadService,
        CoreOptions options,
        TicketService ticketService,
        IHubContext<AppHub, IAppHubEvent> hub,
        IMapper mapper
    ) : base(logger, cache, validator)
    {
        _fileUploadService = fileUploadService;
        TicketService = ticketService;
        Options = options;
        Hub = hub;
        Mapper = mapper;
    }

    /// <summary>
    /// Gets ticket details
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("api/ticket/{id}")]
    [Authorize]
    public async Task<Ticket> Retrieve([FromRoute] int id)
    {
        AuthorizeAny(
            () => Actor.IsObserver,
            () => TicketService.IsOwnerOrTeamMember(id, Actor.Id).Result
        );

        await Cache.SetStringAsync
        (
            $"{"file-permit:"}{Actor.Id}:{id}",
            "true",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = new TimeSpan(0, 15, 0) }
        );

        return await TicketService.Retrieve(id, Actor.Id);
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
        await Validate(model);
        var result = await TicketService.Create(model, Actor.Id, Actor.IsSupport);
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
        AuthorizeAny(
            () => Actor.IsSupport,
            () => TicketService.UserCanUpdate(model.Id, Actor.Id).Result
        );

        await Validate(model);

        // Retrieve the previous ticket result for comparison soon
        var prevTicket = await TicketService.Retrieve(model.Id, Actor.Id);

        var result = await TicketService.Update(model, Actor.Id, Actor.IsSupport);
        // Ignore labels being different
        if (result.Label != prevTicket.Label) prevTicket.LastUpdated = result.LastUpdated;

        // If the ticket hasn't been meaningfully updated, don't send a notification
        if (prevTicket.LastUpdated != result.LastUpdated)
            await Notify(Mapper.Map<TicketNotification>(result), EventAction.Updated);

        return result;
    }

    /// <summary>
    /// Lists tickets based on search params
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet("/api/ticket/list")]
    [Authorize]
    public Task<IEnumerable<TicketSummary>> List([FromQuery] TicketSearchFilter model)
        => TicketService.List(model, Actor.Id, Actor.IsSupport || Actor.IsObserver);

    /// <summary>
    /// Create new ticket comment
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("/api/ticket/comment")]
    [Authorize]
    public async Task<TicketActivity> AddComment([FromForm] NewTicketComment model)
    {
        AuthorizeAny(
            () => Actor.IsObserver,
            () => Actor.IsSupport,
            () => TicketService.IsOwnerOrTeamMember(model.TicketId, Actor.Id).Result
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
        AuthorizeAny(
            () => Actor.IsSupport,
            () => Actor.IsObserver
        );

        return await TicketService.ListLabels(model);
    }

    private Task Notify(TicketNotification notification, EventAction action)
    {
        var ev = new HubEvent<TicketNotification>
        {
            Model = notification,
            Action = action,
            ActingUser = HubEventActingUserDescription.FromUser(Actor)
        };

        var tasks = new List<Task>();

        tasks.Add(Hub.Clients.Group(AppConstants.InternalSupportChannel).TicketEvent(ev));

        if (!string.IsNullOrEmpty(notification.TeamId))
            tasks.Add(Hub.Clients.Group(notification.TeamId).TicketEvent(ev));
        else
            tasks.Add(Hub.Clients.Group(notification.RequesterId).TicketEvent(ev));

        return Task.WhenAll(tasks);
    }
}
