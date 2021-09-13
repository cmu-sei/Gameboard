using System.Threading.Tasks;
using Gameboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers
{

    [Authorize]
    public class ReportController: _Controller
    {
        public ReportController(
            ILogger<ReportController> logger,
            IDistributedCache cache,
            ReportService service
        ): base(logger, cache)
        {
            Service = service;
        }

        ReportService Service { get; }

        [HttpGet("/api/report/userstats")]
        [Authorize]
        public async Task<ActionResult<UserReport>> GetUserStats()
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            return Ok(await Service.GetUserStats());
        }

        [HttpGet("/api/report/playerstats")]
        [Authorize]
        public async Task<ActionResult<PlayerReport>> GetPlayerStats()
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            return Ok(await Service.GetPlayerStats());
        }

        [HttpGet("/api/report/sponsorstats")]
        [Authorize]
        public async Task<ActionResult<SponsorReport>> GetSponsorStats()
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            return Ok(await Service.GetSponsorStats());
        }

        [HttpGet("/api/report/gamesponsorstats/{id}")]
        [Authorize]
        public async Task<ActionResult<GameSponsorReport>> GetGameSponsorsStats([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            return Ok(await Service.GetGameSponsorsStats(id));
        }

        [HttpGet("/api/report/challengestats/{id}")]
        [Authorize]
        public async Task<ActionResult<ChallengeReport>> GetChallengeStats([FromRoute] string id)
        {
            AuthorizeAny(
                () => Actor.IsObserver
            );

            return Ok(await Service.GetChallengeStats(id));
        }
    }
}
