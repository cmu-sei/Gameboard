using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.SystemNotifications;

[Route("api/system-notifications")]
[Authorize]
public class SystemNotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SystemNotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public Task<ViewSystemNotification> CreateSystemNotification([FromBody] CreateSystemNotification createSystemNotification)
        => _mediator.Send(new CreateSystemNotificationCommand(createSystemNotification));

    [HttpGet]
    public Task<IEnumerable<ViewSystemNotification>> GetVisibleNotifications()
        => _mediator.Send(new GetVisibleNotificationsQuery());
}
