using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.Teams;

[Authorize]
[Route("/api/team")]
public class TeamController : ControllerBase
{
    private IActingUserService _actingUserService;
    private ILogger<TeamController> _logger;
    private IMediator _mediator;
    private readonly ITeamService _teamService;

    public TeamController
    (
        IActingUserService actingUserService,
        ILogger<TeamController> logger,
        IMediator mediator,
        ITeamService teamService
    )
    {
        _actingUserService = actingUserService;
        _logger = logger;
        _mediator = mediator;
        _teamService = teamService;
    }

    [HttpGet("{teamId}")]
    public async Task<Team> GetTeam(string teamId)
        => await _mediator.Send(new GetTeamQuery(teamId, _actingUserService.Get()));
}
