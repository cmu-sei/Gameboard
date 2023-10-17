using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Consoles;

[Authorize]
[Route("/api/consoles")]
public class ConsolesController : ControllerBase
{
    private readonly IActingUserService _actingUserService;
    private readonly ILogger<ConsolesController> _logger;
    private readonly IMediator _mediator;

    public ConsolesController
    (
        IActingUserService actingUserService,
        ILogger<ConsolesController> logger,
        IMediator mediator
    )
    {
        _actingUserService = actingUserService;
        _logger = logger;
        _mediator = mediator;
    }

    [HttpPost("active")]
    public Task<ConsoleActionResponse> RecordUserActive(CancellationToken cancellationToken)
        => _mediator.Send(new RecordUserConsoleActiveCommand(_actingUserService.Get()), cancellationToken);
}
