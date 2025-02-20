using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
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
    /// </summary>
    /// <param name="model"></param>
    /// <param name="isCompleted">Whether or not the challenge has ever been completed by the current user (in practice mode).</param>
    /// <returns></returns>
    [HttpGet]
    [AllowAnonymous]
    public Task<SearchPracticeChallengesResult> Browse([FromQuery] SearchFilter model, [FromQuery] bool? isCompleted = null)
        => _mediator.Send(new SearchPracticeChallengesQuery(model, isCompleted));

    [HttpGet("session")]
    public Task<PracticeSession> GetPracticeSession()
        => _mediator.Send(new GetPracticeSessionQuery(_actingUserService.Get().Id));

    [HttpGet]
    [Route("settings")]
    [AllowAnonymous]
    public Task<PracticeModeSettingsApiModel> GetSettings()
        => _mediator.Send(new GetPracticeModeSettingsQuery(_actingUserService.Get()));

    [HttpGet]
    [Route("user/{userId}/history")]
    public Task<UserPracticeHistoryChallenge[]> GetUserPracticeHistory([FromRoute] string userId, CancellationToken cancellationToken)
        => _mediator.Send(new GetUserPracticeHistoryQuery(userId), cancellationToken);

    [HttpPut]
    [Route("settings")]
    public Task UpdateSettings([FromBody] PracticeModeSettingsApiModel settings)
        => _mediator.Send(new UpdatePracticeModeSettingsCommand(settings, _actingUserService.Get()));
}
