using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Consoles.Requests;
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

    [HttpGet]
    [Authorize(AppConstants.ConsolePolicy)]
    public Task<ConsoleState> GetConsole([FromQuery] GetConsoleStateRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new GetConsoleRequest(request.ChallengeId, request.Name), cancellationToken);

    [HttpPost("active")]
    public Task<ConsoleActionResponse> RecordUserActive(CancellationToken cancellationToken)
        => _mediator.Send(new RecordUserConsoleActiveCommand(_actingUserService.Get()), cancellationToken);

    // maybe someday, we should unify this (which is designed to tell us who's using which console) with the "user active" endpoint above
    // (which auto-extends practice sessions)
    [HttpPost("user")]
    public Task SetConsoleActiveUser([FromBody] ConsoleId consoleId, CancellationToken cancellationToken)
        => _mediator.Send(new SetActiveUserCommand(consoleId), cancellationToken);
}
