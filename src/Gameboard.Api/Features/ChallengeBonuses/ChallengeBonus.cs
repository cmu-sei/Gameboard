using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.ChallengeBonuses;

[Authorize]
[Route("api/challenge/{challengeId}/bonus")]
public class ChallengeBonusController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChallengeBonusController(IMediator mediator) : base()
    {
        _mediator = mediator;
    }

    [HttpPost("manual")]
    public async Task<ActionResult> AddManualBonus(CreateManualChallengeBonus model)
    {
        await _mediator.Send(new AddManualBonusCommand(model));
        return Ok();
    }
}
