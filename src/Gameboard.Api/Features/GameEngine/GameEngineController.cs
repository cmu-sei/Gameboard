using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.GameEngine;

[Authorize]
[Route("/api/gameEngine")]
public class GameEngineController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("state")]
    public async Task<IEnumerable<GameEngineGameState>> GetGameStates(string teamId)
        => await _mediator.Send(new GetGameStateQuery(teamId));
}
