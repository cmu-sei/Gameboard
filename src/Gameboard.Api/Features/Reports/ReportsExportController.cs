// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceStack.Text;

namespace Gameboard.Api.Features.Reports;

[ApiController]
[Authorize]
[Route("/api/reports/export")]
public class ReportsExportController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("challenges")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetChallengesReportExport([FromQuery] ChallengesReportParameters parameters)
    {
        var results = await _mediator.Send(new GetChallengesReportExportQuery(parameters));
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }

    [HttpGet("challenges/submissions/{challengeSpecId?}")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetChallengesSubmissionsExport([FromRoute] string challengeSpecId, [FromQuery] ChallengesReportParameters parameters, CancellationToken cancellationToken)
    {
        var results = await _mediator.Send(new GetChallengesReportSubmissionsExport(challengeSpecId, parameters), cancellationToken);
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }

    [HttpGet("enrollment")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetEnrollmentReportExport([FromQuery] EnrollmentReportParameters parameters)
    {
        var results = await _mediator.Send(new EnrollmentReportExportQuery(parameters));
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }

    [HttpGet("feedback")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetFeedbackReportExport([FromQuery] FeedbackReportParameters parameters)
    {
        var results = await _mediator.Send(new FeedbackReportExportQuery(parameters));
        return new FileContentResult(GetReportExport(results.Records), MimeTypes.TextCsv);
    }

    [HttpGet("players")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetPlayersReportExport([FromQuery] PlayersReportParameters parameters)
    {
        var results = await _mediator.Send(new GetPlayersReportExportQuery(parameters));
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }

    [HttpGet("practice-area")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetPracticeModeReportExport([FromQuery] PracticeModeReportParameters parameters, CancellationToken cancellationToken)
    {
        var results = await _mediator.Send(new PracticeModeReportCsvExportQuery(parameters), cancellationToken);
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }

    [HttpGet("practice-area/submissions/{challengeSpecId?}")]
    public async Task<IActionResult> GetPracticeModeReportSubmissionsExport([FromRoute] string challengeSpecId, [FromQuery] PracticeModeReportParameters parameters, CancellationToken cancellationToken)
    {
        var results = await _mediator.Send(new PracticeModeReportSubmissionsExportQuery(challengeSpecId, parameters), cancellationToken);
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }


    [HttpGet("support")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public async Task<IActionResult> GetSupportReport([FromQuery] SupportReportParameters parameters)
    {
        var results = await _mediator.Send(new SupportReportExportQuery(parameters));
        return new FileContentResult(GetReportExport(results), MimeTypes.TextCsv);
    }

    private byte[] GetReportExport<T>(IEnumerable<T> records)
    {
        var csvText = CsvSerializer.SerializeToCsv(records);
        return Encoding.UTF8.GetBytes(csvText.ToString());
    }
}
