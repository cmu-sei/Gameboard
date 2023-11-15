using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Admin;

[Route("api/admin/external-games")]
[Authorize]
public class AdminExternalGamesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminExternalGamesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // [HttpGet("gameId")]
    // public Task<ExternalGameAdminContext> GetExternalGameAdminContext([FromRoute] string gameId)
    // {

    // }
}
