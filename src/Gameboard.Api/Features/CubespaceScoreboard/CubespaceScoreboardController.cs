using System.Threading.Tasks;
using Gameboard.Api.Controllers;
using Gameboard.Api.Features.CubespaceScoreboard;
using Gameboard.Api.Features.UnityGames;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

public class CubespaceScoreboardController : _Controller
{
    private readonly ICubespaceScoreboardService _cubespaceScoreboardService;
    private readonly IUnityGameService _unityGameService;

    public CubespaceScoreboardController(
        IDistributedCache cache,
        ILogger<CubespaceScoreboardController> logger,
        UnityGamesValidator validator,
        ICubespaceScoreboardService cubespaceScoreboardService,
        IUnityGameService unityGameService) : base(logger, cache, validator)
    {
        _cubespaceScoreboardService = cubespaceScoreboardService;
        _unityGameService = unityGameService;
    }

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