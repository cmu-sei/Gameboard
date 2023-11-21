using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Games;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Teams;

[Authorize]
[Route("/api/team")]
public class TeamController : ControllerBase
{
    private readonly IActingUserService _actingUserService;
    private readonly IMediator _mediator;
    private readonly ITeamService _teamService;

    public TeamController
    (
        IActingUserService actingUserService,
        IMediator mediator,
        ITeamService teamService
    )
    {
        _actingUserService = actingUserService;
        _mediator = mediator;
        _teamService = teamService;
    }

    [HttpGet("{teamId}")]
    public async Task<Team> GetTeam(string teamId)
        => await _mediator.Send(new GetTeamQuery(teamId, _actingUserService.Get()));

    /// <summary>
    /// Extend or end a team's session. If no value is supplied for the SessionEnd property of the
    /// body, the session is ended. Otherwise, the session end date/time is updated to the requested 
    /// value (given the appropriate permissions).
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("session")]
    public Task UpdateSession([FromBody] SessionChangeRequest model, CancellationToken cancellationToken)
    {
        if (model.SessionEnd is null)
            return _mediator.Send(new EndTeamSessionCommand(model.TeamId, _actingUserService.Get()), cancellationToken);
        else
            return _teamService.ExtendSession
            (
                new ExtendTeamSessionRequest
                {
                    TeamId = model.TeamId,
                    NewSessionEnd = model.SessionEnd.Value,
                    Actor = _actingUserService.Get()
                },
                cancellationToken
            );
    }

    [HttpPost("{teamId}/session")]
    public async Task ResetSession([FromRoute] string teamId, [FromBody] ResetTeamSessionCommand request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ResetTeamSessionCommand(teamId, request.UnenrollTeam, _actingUserService.Get()), cancellationToken);
    }

    [HttpPut("{teamId}/ready")]
    [Authorize]
    public Task UpdateTeamReadyState([FromRoute] string teamId, [FromBody] UpdateIsReadyModel isReadyCommand)
        => _mediator.Send(new UpdateTeamReadyStateCommand(teamId, isReadyCommand.IsReady));
}
