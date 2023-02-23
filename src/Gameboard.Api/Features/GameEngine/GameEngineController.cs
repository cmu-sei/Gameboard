using System.Threading.Tasks;
using Gameboard.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.GameEngine;

[Authorize]
public class GameEngineController : _Controller
{
    public GameEngineController
    (
        IDistributedCache cache,
        ILogger<GameEngineController> logger,
        GameEngineValidator validator
    ) : base(logger, cache, validator) { }

    public string EngineRouteId { get; }

    public async Task<IGameState> GetGameState(string gameId)
    {

    }
}

