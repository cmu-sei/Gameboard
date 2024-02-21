using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Teams;

[Route("api/admin/team")]
[Authorize]
public class AdminTeamsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminTeamsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPut("session")]
    public Task<AdminExtendTeamSessionResponse> ExtendTeamSessions([FromBody] AdminExtendTeamSessionRequest request)
        => _mediator.Send(request);
}
