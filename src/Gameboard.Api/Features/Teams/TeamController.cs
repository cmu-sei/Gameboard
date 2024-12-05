using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.Start;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Teams;

[ApiController]
[Authorize]
[Route("/api/team")]
public class TeamController(
    IActingUserService actingUserService,
    IMediator mediator,
    ITeamService teamService
) : ControllerBase
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly IMediator _mediator = mediator;
    private readonly ITeamService _teamService = teamService;

    [HttpDelete("{teamId}/players/{playerId}")]
    public Task<RemoveFromTeamResponse> RemovePlayer([FromRoute] string teamId, [FromRoute] string playerId)
        => _mediator.Send(new RemoveFromTeamCommand(playerId));

    [HttpPut("{teamId}/players")]
    public Task<AddToTeamResponse> AddUser([FromRoute] string teamId, [FromBody] AddToTeamCommand request)
        => _mediator.Send(request);

    [HttpGet("{teamId}")]
    public async Task<Team> GetTeam(string teamId)
        => await _mediator.Send(new GetTeamQuery(teamId, _actingUserService.Get()));

    /// <summary>
    /// Loads metadata to support emailing teams in a given game
    /// </summary>
    /// <param name="gameId">Game Id</param>
    /// <returns>TeamSummary[]</returns>
    [HttpGet("/api/teams/{gameId}")]
    [Authorize]
    public Task<IEnumerable<TeamSummary>> GetTeams([FromRoute] string gameId)
        => _mediator.Send(new GetTeamsMailMetadataQuery(gameId));

    /// <summary>
    /// Extend or end a team's session. If no value is supplied for the SessionEnd property of the
    /// body, the session is ended. Otherwise, the session end date/time is updated to the requested 
    /// value (given the appropriate permissions).
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("session")]
    public async Task<TeamSessionUpdate> UpdateSession([FromBody] SessionChangeRequest model, CancellationToken cancellationToken)
    {
        if (model.SessionEnd is null)
            return await _mediator.Send(new EndTeamSessionCommand(model.TeamId, _actingUserService.Get()), cancellationToken);
        else
        {
            var player = await _teamService.ExtendSession
            (
                new ExtendTeamSessionRequest
                {
                    TeamId = model.TeamId,
                    NewSessionEnd = model.SessionEnd.Value,
                    Actor = _actingUserService.Get()
                },
                cancellationToken
            );

            return new TeamSessionUpdate { Id = model.TeamId, SessionEndsAt = player.SessionEnd.ToUnixTimeMilliseconds() };
        }
    }

    [HttpGet("{teamId}/play-state")]
    public Task<GamePlayState> GetTeamGamePlayState([FromRoute] string teamId)
        => _mediator.Send(new GetGamePlayStateQuery(teamId, _actingUserService.Get()?.Id));

    [HttpPut("{teamId}/ready")]
    [Authorize]
    public Task UpdateTeamReadyState([FromRoute] string teamId, [FromBody] UpdateIsReadyRequest isReadyCommand)
        => _mediator.Send(new UpdateTeamReadyStateCommand(teamId, isReadyCommand.IsReady));

    [HttpPut("{teamId}/session")]
    public Task ResetSession([FromRoute] string teamId, [FromBody] ResetTeamSessionCommand request, CancellationToken cancellationToken)
        => _mediator.Send(new ResetTeamSessionCommand(teamId, request.ResetType, _actingUserService.Get()), cancellationToken);

    [HttpGet("{teamId}/timeline")]
    public Task<EventHorizon> GetTeamEventHorizon([FromRoute] string teamId)
        => _mediator.Send(new GetTeamEventHorizonQuery(teamId));
}
