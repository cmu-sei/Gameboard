using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Features.GameEngine.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.GameEngine;

[Authorize]
[Route("/api/gameEngine")]
public class GameEngineController : ControllerBase
{
    private readonly GameEngineService _gameEngine;
    private readonly IMediator _mediator;

    public GameEngineController
    (
        GameEngineService gameEngineService,
        ILogger<GameEngineController> logger,
        IMediator mediator
    )
    {
        _gameEngine = gameEngineService;
        _mediator = mediator;
    }

    [HttpGet("state")]
    public async Task<IEnumerable<GameEngineGameState>> GetGameState(string teamId)
        => await _mediator.Send(new GetGameStateQuery(teamId));

    [HttpGet("submissions")]
    public async Task<IEnumerable<GameEngineSectionSubmission>> GetSubmissions(string teamId, string challengeId)
        => await _mediator.Send(new GetSubmissionsQuery(teamId, challengeId));
}
