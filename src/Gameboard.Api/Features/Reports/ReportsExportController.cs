using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Reports;

[Authorize]
[Route("/api/reports/export")]
public class ReportsExportController : ControllerBase
{
    private readonly User _actingUser;
    private readonly IMediator _mediator;

    public ReportsExportController(IActingUserService actingUserService, IMediator mediator)
    {
        _actingUser = actingUserService.Get();
        _mediator = mediator;
    }

    [HttpGet("challenges")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetChallengesReport(GetChallengesReportQueryArgs parameters)
    {
        var results = await _mediator.Send(new ChallengesReportExportQuery(parameters));
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }

    [HttpGet("enrollment")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetEnrollmentReportExport(EnrollmentReportParameters parameters)
    {
        var results = await _mediator.Send(new EnrollmentReportExportQuery(parameters));
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }

    [HttpGet("players")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetPlayersReport(PlayersReportQueryParameters parameters)
    {
        var results = await _mediator.Send(new PlayersReportExportQuery(parameters));
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }

    [HttpGet("practice-area")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetPracticeModeReportExport(PracticeModeReportParameters parameters, CancellationToken cancellationToken)
    {
        var results = await _mediator.Send(new PracticeModeReportCsvExportQuery(parameters, _actingUser), cancellationToken);
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
