using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.SystemNotifications;

[Route("api")]
[Authorize]
public class SystemNotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SystemNotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("system-notifications")]
    public Task<ViewSystemNotification> CreateSystemNotification([FromBody] CreateSystemNotification createSystemNotification)
        => _mediator.Send(new CreateSystemNotificationCommand(createSystemNotification));

    [HttpGet("system-notifications")]
    public Task<IEnumerable<ViewSystemNotification>> GetVisibleNotifications()
        => _mediator.Send(new GetVisibleNotificationsQuery());

    [HttpGet("admin/system-notifications")]
    public Task<IEnumerable<AdminViewSystemNotification>> GetAllNotifications()
        => _mediator.Send(new GetAdminSystemNotificationsQuery());
}
