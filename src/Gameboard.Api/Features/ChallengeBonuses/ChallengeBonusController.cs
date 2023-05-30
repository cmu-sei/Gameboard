using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.ChallengeBonuses;

[Authorize]
public class ChallengeBonusController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChallengeBonusController(IMediator mediator) : base()
    {
        _mediator = mediator;
    }

    [HttpPut("api/game/{gameId}/bonus/config")]
    public async Task<GameScoringConfig> ConfigureAutomaticBonusesForGame([FromRoute] string gameId, [FromBody] GameAutomaticBonusesConfig config)
        => await _mediator.Send(new ConfigureGameAutoBonusesCommand(new ConfigureGameAutoBonusesCommandParameters
        {
            GameId = gameId,
            Config = config
        }));

    [HttpPost("api/challenge/{challengeId}/bonus/manual")]
    public async Task<ActionResult> AddManualBonus([FromRoute] string challengeId, [FromBody] CreateManualChallengeBonus model)
    {
        await _mediator.Send(new AddManualBonusCommand(challengeId, model));
        return Ok();
    }

    [HttpGet("api/challenge/{challengeId}/bonus/manual")]
    public async Task<IEnumerable<ManualChallengeBonusViewModel>> List([FromRoute] string challengeId)
        => await _mediator.Send(new ListManualBonusesQuery(challengeId));

    [HttpDelete("api/bonus/manual/{manualBonusId}")]
    public async Task DeleteManualBonus(string manualBonusId)
        => await _mediator.Send(new DeleteManualBonusCommand(manualBonusId));

}
