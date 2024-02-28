using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Admin;

[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stats")]
    public Task<GetSiteOverviewStatsResponse> GetSiteOverviewStats()
        => _mediator.Send(new GetSiteOverviewStatsQuery());

    [HttpGet("active-challenges")]
    public Task<GetAppActiveChallengesResponse> GetActiveChallenges([FromQuery] string playerMode)
        => _mediator.Send(new GetAppActiveChallengesQuery(playerMode.ToLower() == "practice" ? PlayerMode.Practice : PlayerMode.Competition));
}
