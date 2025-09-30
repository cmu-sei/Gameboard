// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.SystemNotifications;

[ApiController]
[Route("api")]
[Authorize]
public class SystemNotificationsController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost("system-notifications")]
    public Task<ViewSystemNotification> CreateSystemNotification([FromBody] CreateSystemNotification createSystemNotification)
        => _mediator.Send(new CreateSystemNotificationCommand(createSystemNotification));

    [HttpGet("system-notifications")]
    public Task<IEnumerable<ViewSystemNotification>> GetVisibleNotifications()
        => _mediator.Send(new GetVisibleNotificationsQuery());

    [HttpPut("system-notifications/{id}")]
    public Task<ViewSystemNotification> UpdateSystemNotification([FromRoute] string id, [FromBody] UpdateSystemNotificationRequest request)
        => _mediator.Send(new UpdateSystemNotificationCommand(request));

    [HttpDelete("system-notifications/{id}")]
    public Task DeleteSystemNotification([FromRoute] string id)
        => _mediator.Send(new DeleteSystemNotificationCommand(id));

    [HttpPost("system-notifications/interaction")]
    public Task UpdateInteractions([FromBody] UpdateInteractionRequest request)
        => _mediator.Send(new UpdateUserSystemNotificationInteractionCommand(request.SystemNotificationIds, request.InteractionType));

    [HttpGet("admin/system-notifications")]
    public Task<IEnumerable<AdminViewSystemNotification>> GetAllNotifications()
        => _mediator.Send(new GetAdminSystemNotificationsQuery());
}
