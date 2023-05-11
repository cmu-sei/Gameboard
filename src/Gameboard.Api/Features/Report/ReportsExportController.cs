using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Reports;

[Authorize]
[Route("/api/reports/export")]
public class ReportsExportController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IReportsService _reportsService;

    public ReportsExportController(IMediator mediator, IReportsService reportsService)
    {
        _mediator = mediator;
        _reportsService = reportsService;
    }

    [HttpGet("challenges-report")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetChallengesReport(GetChallengesReportQueryArgs parameters)
    {
        var results = await _mediator.Send(new ChallengesReportExportQuery(parameters));
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }

    [HttpGet("players-report")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetPlayersReport(PlayersReportQueryParameters parameters)
    {
        var results = await _mediator.Send(new PlayersReportExportQuery(parameters));
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }

    [HttpGet("support-report")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetSupportReport(SupportReportParameters parameters)
    {
        var results = await _mediator.Send(new SupportReportExportQuery(parameters));
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }

    private byte[] GetReportExport<T>(IEnumerable<T> records)
    {
        var csvText = ServiceStack.StringExtensions.ToCsv(records);
        return Encoding.UTF8.GetBytes(csvText.ToString());
    }
}
