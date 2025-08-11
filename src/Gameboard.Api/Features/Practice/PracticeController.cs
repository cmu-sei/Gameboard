using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Practice.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Practice;

[ApiController]
[Authorize]
[Route("/api/practice")]
public class PracticeController(IActingUserService actingUserService, IMediator mediator) : ControllerBase
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Search challenges within games that have been set to Practice mode.
    /// 
    /// This is the public-facing endpoint which supports anonymous and unprivileged search, and orders by amorphous text vector vibes. The elevated endpoint, for admin tasks,
    /// is below.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="userProgress">Whether or not the challenge has ever been attempted/completed by the current user (in practice mode).</param>
    /// <returns></returns>
    [HttpGet]
    [AllowAnonymous]
    public Task<SearchPracticeChallengesResult> Search([FromQuery] SearchFilter filter, [FromQuery] SearchPracticeChallengesRequestUserProgress? userProgress = null)
        => _mediator.Send(new SearchPracticeChallengesQuery(filter, userProgress));

    [HttpGet("challenge/list")]
    public Task<PracticeChallengeView[]> ListChallenges([FromQuery] ListChallengesRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new ListChallengesQuery(request), cancellationToken);

    [HttpGet("challenge-group/{id}")]
    public Task<GetPracticeChallengeGroupResponse> GetGroup([FromRoute] string id, CancellationToken cancellationToken)
        => _mediator.Send(new GetPracticeChallengeGroupQuery(id), cancellationToken);

    [HttpGet("challenge-group/list")]
    public Task<ListPracticeChallengeGroupsResponse> ListGroups([FromQuery] ListPracticeChallengeGroupsRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new ListPracticeChallengeGroupsQuery(request), cancellationToken);

    [HttpPost("challenge-group")]
    public Task<PracticeChallengeGroupDto> CreatePracticeChallengeGroup([FromForm] CreatePracticeChallengeGroupRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new CreatePracticeChallengeGroupCommand(request), cancellationToken);

    [HttpPut("challenge-group")]
    public Task<PracticeChallengeGroupDto> UpdatePracticeChallengeGroup([FromForm] UpdatePracticeChallengeGroupRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new UpdatePracticeChallengeGroupCommand(request), cancellationToken);

    /// <summary>
    /// Given one or more practice challenge collections, get the user's overall progress and performance on them and any subcollections
    /// they contain (including completions, attempts, and certificates.)
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("challenge-group/user-data")]
    public Task<GetPracticeChallengeGroupsUserDataResponse> GetUserChallengeGroups([FromQuery] GetPracticeChallengeGroupUserDataRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new GetPracticeChallengeGroupsUserDataQuery(request.UserId, request.ChallengeGroupIds), cancellationToken);

    [HttpDelete("challenge-group/{id}")]
    public Task DeletePracticeChallengeGroup([FromRoute] string id, CancellationToken cancellationToken)
        => _mediator.Send(new DeletePracticeChallengeGroupCommand(id), cancellationToken);

    [HttpPost("challenge-group/{id}/challenges")]
    public Task<AddChallengesToGroupResponse> AddChallengesToGroup([FromRoute] string id, [FromBody] AddChallengesToGroupRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new AddChallengesToGroupCommand(id, request), cancellationToken);

    [HttpDelete("challenge-group/{id}/challenges")]
    public Task RemoveChallengesFromGroup([FromRoute] string id, [FromBody] RemoveChallengesFromGroupRequest request, CancellationToken cancellationToken)
        => _mediator.Send(new RemoveChallengesFromGroupCommand(id, request.ChallengeSpecIds), cancellationToken);

    [HttpGet("challenge-tags")]
    public Task<ListChallengeTagsResponse> ListTags(CancellationToken cancellationToken)
        => _mediator.Send(new ListChallengeTagsQuery(), cancellationToken);

    [HttpGet("games")]
    public Task<ListGamesResponse> ListGames(CancellationToken cancellationToken)
        => _mediator.Send(new ListGamesQuery(), cancellationToken);

    [HttpGet("session")]
    public Task<PracticeSession> GetPracticeSession(CancellationToken cancellationToken)
        => _mediator.Send(new GetPracticeSessionQuery(_actingUserService.Get().Id), cancellationToken);

    [HttpGet]
    [Route("settings")]
    [AllowAnonymous]
    public Task<PracticeModeSettingsApiModel> GetSettings(CancellationToken cancellationToken)
        => _mediator.Send(new GetPracticeModeSettingsQuery(_actingUserService.Get()), cancellationToken);

    [HttpPut]
    [Route("settings")]
    public Task UpdateSettings([FromBody] PracticeModeSettingsApiModel settings)
        => _mediator.Send(new UpdatePracticeModeSettingsCommand(settings, _actingUserService.Get()));

    [HttpGet]
    [Route("user/{userId}/history")]
    public Task<UserPracticeHistoryChallenge[]> GetUserPracticeHistory([FromRoute] string userId, CancellationToken cancellationToken)
        => _mediator.Send(new GetUserPracticeHistoryQuery(userId), cancellationToken);

    [HttpGet]
    [Route("user/{userId}/summary")]
    public Task<GetUserPracticeSummaryResponse> GetUserPracticeSummary([FromRoute] string userId, CancellationToken cancellationToken)
        => _mediator.Send(new GetUserPracticeSummaryRequest(userId), cancellationToken);
}
