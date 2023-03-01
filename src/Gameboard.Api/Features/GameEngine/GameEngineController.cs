using System;
using System.Threading.Tasks;
using Gameboard.Api.Controllers;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.GameEngine;

[Authorize]
[Route("/api/gameEngine")]
public class GameEngineController : _Controller
{
    private readonly GameEngineService _gameEngine;
    private readonly IGameboardMediator<GetGameStateRequest, GameEngineGameState> _mediator;
    private readonly GetGameStateValidator _getGameStateValidator;

    public GameEngineController
    (
        GameEngineService gameEngineService,
        IDistributedCache cache,
        ILogger<GameEngineController> logger,
        GameEngineValidator validator,
        IGameboardMediator<GetGameStateRequest, GameEngineGameState> mediator,
        GetGameStateValidator getGameStateValidator
    ) : base(logger, cache, validator)
    {
        _gameEngine = gameEngineService;
        _getGameStateValidator = getGameStateValidator;
        _mediator = mediator;
    }

    [HttpGet("state")]
    public async Task<GameEngineGameState> GetGameState(string teamId)
    {
        return await _mediator.Send(new GetGameStateRequest(teamId), context =>
        {
            context.AuthorizationRules.AddAllowedRoles
            (
                UserRole.Designer,
                UserRole.Support,
                UserRole.Observer,
                UserRole.Admin
            );

            context.Validators.Add(_getGameStateValidator);
        });
    }
}
