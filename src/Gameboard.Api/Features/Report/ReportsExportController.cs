using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    [HttpGet("players-report")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetPlayersReport(PlayersReportQueryParameters parameters)
    {
        var results = await _mediator.Send(new GetPlayersReportExportQuery(parameters));
        return new FileContentResult(GetReportExport(results), "text/csv");
    }

    private byte[] GetReportExport<T>(IEnumerable<T> records)
    {
        var csvText = ServiceStack.StringExtensions.ToCsv(records);
        return Encoding.UTF8.GetBytes(csvText.ToString());
    }
}
