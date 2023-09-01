using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Practice;

[Authorize]
[Route("/api/practice")]
public class PracticeController : ControllerBase
{
    private readonly IActingUserService _actingUserService;
    private readonly IHtmlToImageService _htmlToPdfService;
    private readonly IMediator _mediator;

    public PracticeController
    (
        IActingUserService actingUserService,
        IHtmlToImageService htmlToPdfService,
        IMediator mediator
    )
    {
        _actingUserService = actingUserService;
        _htmlToPdfService = htmlToPdfService;
        _mediator = mediator;
    }

    /// <summary>
    /// Search challenges within games that have been set to Practice mode.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpGet]
    [AllowAnonymous]
    public Task<SearchPracticeChallengesResult> Browse([FromQuery] SearchFilter model)
        => _mediator.Send(new SearchPracticeChallengesQuery(model));

    [HttpGet]
    [Route("challenges")]
    public Task<IEnumerable<GameCardContext>> ListChallenges()
        => Task.FromResult(Array.Empty<GameCardContext>().AsEnumerable());

    [HttpGet]
    [Route("settings")]
    public Task<PracticeModeSettings> GetSettings()
        => _mediator.Send(new GetPracticeModeSettingsQuery(_actingUserService.Get()));

    [HttpPut]
    [Route("settings")]
    public Task UpdateSettings([FromBody] UpdatePracticeModeSettings settings)
        => _mediator.Send(new UpdatePracticeModeSettingsCommand(settings, _actingUserService.Get()));
}
