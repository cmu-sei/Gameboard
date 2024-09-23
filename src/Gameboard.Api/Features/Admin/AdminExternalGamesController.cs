using System.Threading.Tasks;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.External;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Admin;

[Route("api/admin/games/external")]
[Authorize]
public class AdminExternalGamesController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("{gameId}")]
    public Task<ExternalGameState> GetExternalGameAdminContext([FromRoute] string gameId)
        => _mediator.Send(new GetExternalGameAdminContextRequest(gameId));

    [HttpPost("{gameId}/pre-deploy")]
    public Task PreDeployGame([FromRoute] string gameId, [FromBody] DeployGameResourcesBody body)
        => _mediator.Send(new DeployGameResourcesCommand(gameId, body?.TeamIds));
}
