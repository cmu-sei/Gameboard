using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
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
        => _mediator.Send(new GetExternalTeamDataQuery(teamId, _actingUserService.Get()));

    [HttpGet("hosts/{hostId}")]
    public Task<GetExternalGameHostsResponseHost> GetHost([FromRoute] string hostId)
        => _mediator.Send(new GetExternalGameHostQuery(hostId));

    [HttpGet("hosts")]
    public Task<GetExternalGameHostsResponse> GetHosts()
        => _mediator.Send(new GetExternalGameHostsQuery());

    [HttpPost("hosts")]
    public Task<ExternalGameHost> UpsertHost([FromBody] UpsertExternalGameHost host)
        => _mediator.Send(new UpsertExternalGameHostCommand(host));

    // [HttpDelete("hosts/{hostId}")]
    // public Task DeleteHost(string hostId)
    //     => _mediator.Send(new DeleteExternalGameHostCommand(hostId));
}
