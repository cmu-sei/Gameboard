using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Reports;

[Authorize]
[Route("/api/report")]
public class ReportController : ControllerBase
{

    [HttpGet("participation-report")]
    public Task<ParticipationReport> GetParticipationReport()
    {
        return Task.FromResult<ParticipationReport>(null);
    }
}
