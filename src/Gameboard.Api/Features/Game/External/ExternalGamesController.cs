using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Games.External;

[Authorize]
[Route("api/games/external")]
public class ExternalGamesController : ControllerBase
{
    private readonly IActingUserService _actingUserService;
    private readonly IMediator _mediator;

    public ExternalGamesController(IActingUserService actingUserService, IMediator mediator)
    {
        _actingUserService = actingUserService;
        _mediator = mediator;
    }

    [HttpGet("team/{teamId}")]
    public Task<GetExternalTeamDataResponse> GetExternalTeamData([FromRoute] string teamId)
    {
        return _mediator.Send(new GetExternalTeamDataQuery(teamId, _actingUserService.Get()));
    }
}
