using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Teams;

[Authorize]
[Route("/api/team")]
public class TeamController : ControllerBase
{
    private readonly IActingUserService _actingUserService;
    private readonly IMediator _mediator;

    public TeamController
    (
        IActingUserService actingUserService,
        IMediator mediator
    )
    {
        _actingUserService = actingUserService;
        _mediator = mediator;
    }

    [HttpGet("{teamId}")]
    public async Task<Team> GetTeam(string teamId)
        => await _mediator.Send(new GetTeamQuery(teamId, _actingUserService.Get()));

    [HttpPost("{teamId}/session")]
    [Authorize]
    public async Task ResetSession([FromRoute] string teamId, [FromBody] ResetSessionCommand request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ResetSessionCommand(teamId, request.Unenroll, _actingUserService.Get()), cancellationToken);
    }
}
