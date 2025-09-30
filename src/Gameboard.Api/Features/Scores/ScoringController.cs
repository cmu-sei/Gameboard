// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Scores;

[ApiController]
[Authorize]
[Route("api")]
public class ScoringController(IMediator mediator) : ControllerBase()
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("game/{gameId}/score/config")]
    public async Task<GameScoringConfig> GetGameScoringConfig([FromRoute] string gameId)
        => await _mediator.Send(new GameScoringConfigQuery(gameId));

    [HttpGet("game/{gameId}/score")]
    [AllowAnonymous]
    public async Task<GameScore> GetGameScore([FromRoute] string gameId)
        => await _mediator.Send(new GameScoreQuery(gameId));

    [HttpGet("game/{gameId}/scoreboard")]
    [AllowAnonymous]
    public Task<ScoreboardData> GetScoreboard([FromRoute] string gameId)
        => _mediator.Send(new GetScoreboardQuery(gameId));

    [HttpGet("challenge/{challengeId}/score")]
    public async Task<TeamChallengeScore> GetChallengeScore([FromRoute] string challengeId)
        => await _mediator.Send(new TeamChallengeScoreQuery(challengeId));

    [HttpGet("team/{teamId}/score")]
    [AllowAnonymous]
    public async Task<TeamScoreQueryResponse> GetTeamScore([FromRoute] string teamId)
        => await _mediator.Send(new GetTeamScoreQuery(teamId));
}
