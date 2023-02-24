using System;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.GameEngine;

[Authorize]
[Route("/api/gameEngine/{gameEngineId:alpha}")]
public class GameEngineController : _Controller
{
    private readonly GameEngineService _gameEngine;

    public GameEngineController
    (
        GameEngineService gameEngineService,
        IDistributedCache cache,
        ILogger<GameEngineController> logger,
        GameEngineValidator validator
    ) : base(logger, cache, validator)
    {
        _gameEngine = gameEngineService;
    }

    private GameEngineType _gameEngineType;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);

        var result = Enum.TryParse<GameEngineType>(context.RouteData.Values["gameEngineId"].ToString(), ignoreCase: true, out _gameEngineType);

        if (!result)
            context.Result = NotFound();

        return;
    }

    [HttpGet("state/team/{teamId:guid}")]
    public async Task<IGameEngineGameState> GetGameState(string teamId)
    {
        AuthorizeAny(
            () => Actor.IsDesigner,
            () => Actor.IsSupport,
            () => Actor.IsObserver,
            () => Actor.IsAdmin
        );

        return await _gameEngine.GetGameState(teamId);
    }
}
