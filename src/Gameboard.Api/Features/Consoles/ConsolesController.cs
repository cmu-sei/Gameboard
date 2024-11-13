using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Consoles;

[ApiController]
[Authorize(AppConstants.ConsolePolicy)]
[Route("/api/consoles")]
public class ConsolesController(
    IActingUserService actingUserService,
    IMediator mediator
    ) : ControllerBase
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IMediator _mediator = mediator;

    [HttpPost("active")]
    public Task<ConsoleActionResponse> RecordUserActive(CancellationToken cancellationToken)
        => _mediator.Send(new RecordUserConsoleActiveCommand(_actingUserService.Get()), cancellationToken);
}
