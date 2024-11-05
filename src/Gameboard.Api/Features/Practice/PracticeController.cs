using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Practice;

[Authorize]
[Route("/api/practice")]
public class PracticeController(
    IActingUserService actingUserService,
    IMediator mediator
    ) : ControllerBase
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IMediator _mediator = mediator;

    /// <summary>
    /// Search challenges within games that have been set to Practice mode.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet]
    [AllowAnonymous]
    public Task<SearchPracticeChallengesResult> Browse([FromQuery] SearchFilter model)
        => _mediator.Send(new SearchPracticeChallengesQuery(model));

    [HttpGet("session")]
    public Task<PracticeSession> GetPracticeSession()
        => _mediator.Send(new GetPracticeSessionQuery(_actingUserService.Get().Id));

    [HttpGet]
    [Route("settings")]
    [AllowAnonymous]
    public Task<PracticeModeSettingsApiModel> GetSettings()
        => _mediator.Send(new GetPracticeModeSettingsQuery(_actingUserService.Get()));

    [HttpPut]
    [Route("settings")]
    public Task UpdateSettings([FromBody] PracticeModeSettingsApiModel settings)
        => _mediator.Send(new UpdatePracticeModeSettingsCommand(settings, _actingUserService.Get()));
}
