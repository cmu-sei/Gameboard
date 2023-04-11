using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Features.Api.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Reports;

[Authorize]
[Route("/api/report")]
public class ReportController : ControllerBase
{
    private readonly IReportStore _store;

    public ReportController(IReportStore store)
    {
        _store = store;
    }

    [HttpGet("participation-report")]
    public Task<ParticipationReport> GetParticipationReport()
    {
        return Task.FromResult<ParticipationReport>(null);
    }

    [HttpGet("parameter/competitions")]
    public Task<IEnumerable<string>> GetCompetitions()
        => _store.GetCompetitions();

    [HttpGet("parameter/tracks")]
    public Task<IEnumerable<string>> GetTracks()
        => _store.GetTracks();
}
