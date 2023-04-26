using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Reports;

[Authorize]
[Route("/api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IReportsService _service;

    public ReportsController(IMediator mediator, IReportsService service)
    {
        _mediator = mediator;
        _service = service;
    }

    [HttpGet]
    public async Task<IEnumerable<Report>> List()
        => await _service.List();

    [HttpGet("{reportKey}/parameter-options")]
    public async Task<ReportParameterOptions> GetOptions([FromRoute] string reportKey, [FromQuery] ReportParameters reportParams)
    {
        return await _mediator.Send(new GetReportParameterOptionsQuery(reportKey, reportParams));
    }

    [HttpGet("participation-report")]
    public Task<ParticipationReport> GetParticipationReport()
    {
        return Task.FromResult<ParticipationReport>(null);
    }

    [HttpGet("challenges-report")]
    public async Task<ChallengesReportResults> GetChallengeReport([FromQuery] GetChallengesReportQueryArgs args)
    {
        return await _mediator.Send(new GetChallengeReportQuery(args));
    }

    [HttpGet("parameter/competitions")]
    public Task<IEnumerable<string>> GetCompetitions()
        => _service.ListParameterOptionsCompetitions();

    [HttpGet("parameter/tracks")]
    public Task<IEnumerable<string>> GetTracks()
        => _service.ListParameterOptionsTracks();
}
