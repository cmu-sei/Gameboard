// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

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
    public Task<GetConsoleResponse> GetConsole([FromQuery] GetConsoleStateRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new GetConsoleRequest(request.ChallengeId, request.Name), cancellationToken);

    [HttpGet("list")]
    [Authorize]
    public Task<ListConsolesResponse> ListConsoles([FromQuery] ListConsolesQuery query, CancellationToken cancellationToken)
        => _mediator.Send(query, cancellationToken);

    [HttpPost("active")]
    public Task<ConsoleActionResponse> RecordUserActive([FromBody] ConsoleId consoleId, CancellationToken cancellationToken)
        => _mediator.Send(new RecordUserConsoleActiveCommand(consoleId, _actingUserService.Get()), cancellationToken);

    // maybe someday, we should unify this (which is designed to tell us who's using which console) with the "user active" endpoint above
    // (which auto-extends practice sessions)
    [HttpPost("user")]
    public Task SetConsoleActiveUser([FromBody] ConsoleId consoleId, CancellationToken cancellationToken)
        => _mediator.Send(new SetActiveUserCommand(consoleId), cancellationToken);
}
