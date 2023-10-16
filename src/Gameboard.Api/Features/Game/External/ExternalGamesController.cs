using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Games.External;

[Authorize]
[Route("api/games/external")]
public class ExternalGamesController : ControllerBase
{
    [HttpGet("player/{id}/metadata")]
    public Task GetPlayerMetadata([FromRoute] string playerId)
    {
        return Task.CompletedTask;
    }
}
