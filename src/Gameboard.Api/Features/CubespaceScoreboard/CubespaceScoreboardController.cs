using System.Threading.Tasks;
using Gameboard.Api.Controllers;
using Gameboard.Api.Features.UnityGames;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Features.CubespaceScoreboard;

public class CubespaceScoreboardController : _Controller
{
    private readonly ICubespaceScoreboardService _cubespaceScoreboardService;

    public CubespaceScoreboardController
    (
        IDistributedCache cache,
        ILogger<CubespaceScoreboardController> logger,
        UnityGamesValidator validator,
        ICubespaceScoreboardService cubespaceScoreboardService
    ) : base(logger, cache, validator) => (_cubespaceScoreboardService) = (cubespaceScoreboardService);

    [HttpPost("/api/cubespace/scoreboard")]
    [AllowAnonymous]
    public async Task<JsonResult> GetScoreboard([FromBody] CubespaceScoreboardRequestPayload payload)
    {
        return new JsonResult(await _cubespaceScoreboardService.GetScoreboard(payload));
    }

    [HttpPost("/api/cubespace/scoreboard/cache-invalidate")]
    [Authorize]
    public void InvalidateScoreboard()
    {
        _cubespaceScoreboardService.InvalidateScoreboardCache();
    }
}
