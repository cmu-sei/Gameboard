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

    [HttpGet("active-challenges")]
    public Task<GetAppActiveChallengesResponse> GetActiveChallenges([FromQuery] string playerMode)
        => _mediator.Send(new GetAppActiveChallengesQuery(playerMode.ToLower() == "practice" ? PlayerMode.Practice : PlayerMode.Competition));

    [HttpGet("active-teams")]
    public Task<GetAppActiveTeamsResponse> GetActiveTeams()
        => _mediator.Send(new GetAppActiveTeamsQuery());

    [HttpPost("announce")]
    public Task CreateAnnouncement([FromBody] SendAnnouncementCommand request)
        => _mediator.Send(request);

    [HttpGet("stats")]
    public Task<GetAppOverviewStatsResponse> GetAppOverviewStats()
        => _mediator.Send(new GetAppOverviewStatsQuery());
}
