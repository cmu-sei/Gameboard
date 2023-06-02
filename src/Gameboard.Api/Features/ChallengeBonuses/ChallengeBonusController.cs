using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.ChallengeBonuses;

[Route("api")]
[Authorize]
public class ChallengeBonusController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChallengeBonusController(IMediator mediator) : base()
    {
        _mediator = mediator;
    }

    [HttpPut("game/{gameId}/bonus/config")]
    public async Task<GameScoringConfig> ConfigureAutomaticBonusesForGame([FromRoute] string gameId, [FromBody] GameAutomaticBonusesConfig config)
        => await _mediator.Send(new ConfigureGameAutoBonusesCommand(new ConfigureGameAutoBonusesCommandParameters
        {
            GameId = gameId,
            Config = config
        }));

    [HttpDelete("game/{gameId}/bonus/config")]
    public async Task DeleteAutomaticBonusesForGame([FromRoute] string gameId)
        => await _mediator.Send(new DeleteGameAutoBonusesConfigCommand(gameId));

    [HttpPost("challenge/{challengeId}/bonus/manual")]
    public async Task<ActionResult> AddManualBonus([FromRoute] string challengeId, [FromBody] CreateManualChallengeBonus model)
    {
        await _mediator.Send(new AddManualBonusCommand(challengeId, model));
        return Ok();
    }

    [HttpGet("challenge/{challengeId}/bonus/manual")]
    public async Task<IEnumerable<ManualChallengeBonusViewModel>> List([FromRoute] string challengeId)
        => await _mediator.Send(new ListManualBonusesQuery(challengeId));

    [HttpDelete("bonus/manual/{manualBonusId}")]
    public async Task DeleteManualBonus(string manualBonusId)
        => await _mediator.Send(new DeleteManualBonusCommand(manualBonusId));

}
