using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Scores;

[Authorize]
[Route("api")]
public class ScoringController : ControllerBase
{
    private readonly IMediator _mediator;

    public ScoringController(IMediator mediator) : base()
    {
        _mediator = mediator;
    }

    // TODO: more rest-correct url? [HttpGet("team/{teamId}/score/{challengeId}")]
    [HttpGet("challenge/{challengeId}/score")]
    public async Task<TeamChallengeScoreSummary> GetTeamChallengeScoreSummary([FromRoute] string challengeId)
        => await _mediator.Send(new TeamChallengeScoreQuery(challengeId));

    [HttpGet("team/{teamId}/score")]
    public async Task<TeamGameScoreSummary> GetTeamGameScoreSummary([FromRoute] string teamId)
        => await _mediator.Send(new TeamGameScoreQuery(teamId));
}
