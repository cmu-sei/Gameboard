using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Admin;

[ApiController]
[Route("api/admin")]
public class AdminController(IMediator mediator, IActingUserService actingUserService) : ControllerBase
{
    private readonly User _actingUser = actingUserService.Get();
    private readonly IMediator _mediator = mediator;

    [HttpGet("active-challenges")]
    public Task<GetAppActiveChallengesResponse> GetActiveChallenges([FromQuery] string playerMode)
        => _mediator.Send(new GetAppActiveChallengesQuery(playerMode.ToLower() == "practice" ? PlayerMode.Practice : PlayerMode.Competition));

    [HttpGet("active-teams")]
    public Task<GetAppActiveTeamsResponse> GetActiveTeams()
        => _mediator.Send(new GetAppActiveTeamsQuery());

    [HttpPost("announce")]
    public Task CreateAnnouncement([FromBody] SendAnnouncementCommand request)
        => _mediator.Send(request);

    [HttpGet("games/{gameId}/game-center")]
    public Task<GameCenterContext> GetGameCenterContext([FromRoute] string gameId)
        => _mediator.Send(new GetGameCenterContextQuery(gameId));

    [HttpGet("games/{gameId}/game-center/practice")]
    public Task<GameCenterPracticeContext> GetGameCenterPracticeContext([FromRoute] string gameId, [FromQuery] GetGameCenterPracticeContextRequest query)
        => _mediator.Send(new GetGameCenterPracticeContextQuery(gameId, query?.SearchTerm, query?.SessionStatus, query?.Sort));

    [HttpGet("games/{gameId}/game-center/teams")]
    public Task<GameCenterTeamsResults> GetGameCenterTeams([FromRoute] string gameId, [FromQuery] GetGameCenterTeamsArgs queryArgs, [FromQuery] PagingArgs pagingArgs)
        => _mediator.Send(new GetGameCenterTeamsQuery(gameId, queryArgs, pagingArgs));

    [HttpGet("games/{gameId}/players/export")]
    public Task<GetPlayersCsvExportResponse> GetPlayersCsvExport([FromRoute] string gameId, [FromQuery] string teamIds)
        => _mediator.Send(new GetPlayersCsvExportQuery(gameId, teamIds.IsEmpty() ? null : teamIds.Split(',')));

    [HttpPut("players/{playerId}/name")]
    public Task ApprovePlayerName([FromRoute] string playerId, [FromBody] ApprovePlayerNameRequest request)
        => _mediator.Send(new ApprovePlayerNameCommand(playerId, request.Name, request.RevisionReason));

    [HttpGet("stats")]
    public Task<GetAppOverviewStatsResponse> GetAppOverviewStats()
        => _mediator.Send(new GetAppOverviewStatsQuery());

    [HttpGet("teams/{teamId}")]
    public Task<TeamCenterContext> GetTeamCenterContext([FromRoute] string teamId)
        => _mediator.Send(new GetTeamCenterContextQuery(teamId, _actingUser));
}
