using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Practice;

[Authorize]
[Route("/api/practice")]
public class PracticeController : ControllerBase
{
    private readonly IMediator _mediator;

    public PracticeController(IMediator mediator)
    {
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

    /// <summary>
    /// Get the user's current active practice challenge.
    ///
    /// NOTE: We currently only allow them to have one active at a time, but this may change soon.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpGet("user/{userId}/challenges/active")]
    public Task<IEnumerable<UserChallengeSlim>> GetUserActivePracticeChallenges([FromRoute] string userId)
            => _mediator.Send(new GetUserActiveChallengesQuery(userId));
}
