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

        [HttpGet("/api/report/sponsor")]
        public async Task<ActionResult<SponsorReport>> GetSponsorStats([FromQuery]string gameId)
        {
            return Ok(
                await Service.GetSponsorStats(gameId)
            );
        }
    }
}
