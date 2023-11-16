using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Games.External;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Admin;

[Route("api/admin/games/external")]
[Authorize]
public class AdminExternalGamesController : ControllerBase
{
    private readonly IActingUserService _actingUserService;
    private readonly IMediator _mediator;

    public AdminExternalGamesController(IActingUserService actingUserService, IMediator mediator)
    {
        _actingUserService = actingUserService;
        _mediator = mediator;
    }

    [HttpGet("{gameId}")]
    public Task<ExternalGameAdminContext> GetExternalGameAdminContext([FromRoute] string gameId)
        => _mediator.Send(new GetExternalGameAdminContextRequest(gameId));

    [HttpPost("{gameId}/pre-deploy")]
    public Task PreDeployGame([FromRoute] string gameId)
        => _mediator.Send(new PreDeployExternalGameResourcesCommand(gameId, _actingUserService.Get()));
}
