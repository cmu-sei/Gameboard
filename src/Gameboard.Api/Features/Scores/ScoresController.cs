using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Scores;

[Authorize]
[Route("api")]
public class ScoresController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScoresController(IMediator mediator) : base()
    {
        _mediator = mediator;
    }


    [HttpGet("challenge/{challengeId}/score")]
    public async Task<TeamChallengeScoreSummary> GetTeamChallengeScoreSummary([FromRoute] string challengeId)
        => await _mediator.Send(new TeamChallengeScoreQuery(challengeId));
}
