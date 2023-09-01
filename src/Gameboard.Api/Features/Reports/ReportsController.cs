using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Reports;

[Authorize]
[Route("/api/reports")]
public class ReportsController : ControllerBase
{
    private readonly User _actingUser;
    private readonly IMediator _mediator;
    private readonly IReportsService _service;

    public ReportsController
    (
        IActingUserService actingUserService,
        IMediator mediator,
        IReportsService service
    )
    {
        _actingUser = actingUserService.Get();
        _mediator = mediator;
        _service = service;
    }

    [HttpGet]
    public async Task<IEnumerable<ReportViewModel>> List()
        => await _service.List();

    [HttpGet("challenges-report")]
    public Task<ReportResults<ChallengesReportRecord>> GetChallengeReport([FromQuery] GetChallengesReportQueryArgs args)
        => _mediator.Send(new ChallengesReportQuery(args, _actingUser));

    [HttpGet("enrollment")]
    public Task<ReportResults<EnrollmentReportStatSummary, EnrollmentReportRecord>> GetEnrollmentReport([FromQuery] EnrollmentReportParameters parameters, [FromQuery] PagingArgs paging)
        => _mediator.Send(new EnrollmentReportQuery(parameters, paging, _actingUser));

    [HttpGet("enrollment/trend")]
    public Task<IDictionary<DateTimeOffset, EnrollmentReportLineChartGroup>> GetEnrollmentReportLineChart([FromQuery] EnrollmentReportParameters parameters)
        => _mediator.Send(new EnrollmentReportLineChartQuery(parameters, _actingUser));

    [HttpGet("practice-area")]
    public async Task<ReportResults<PracticeModeReportOverallStats, IPracticeModeReportRecord>> GetPracticeModeReport([FromQuery] PracticeModeReportParameters parameters, [FromQuery] PagingArgs paging)
        => await _mediator.Send(new PracticeModeReportQuery(parameters, _actingUser, paging));

    [HttpGet("practice-area/user/{id}/summary")]
    public async Task<PracticeModeReportPlayerModeSummary> GetPracticeModeReportPlayerModeSummary([FromRoute] string id, [FromQuery] bool isPractice)
        => await _mediator.Send(new PracticeModeReportPlayerModeSummaryQuery(id, isPractice, _actingUser));

    [HttpGet("players-report")]
    public async Task<ReportResults<PlayersReportRecord>> GetPlayersReport([FromQuery] PlayersReportQueryParameters reportParams)
        => await _mediator.Send(new PlayersReportQuery(reportParams, _actingUser));

    [HttpGet("support")]
    public async Task<ReportResults<SupportReportRecord>> GetSupportReport([FromQuery] SupportReportParameters reportParams, [FromQuery] PagingArgs pagingArgs)
        => await _mediator.Send(new SupportReportQuery(reportParams, pagingArgs, _actingUser));

    [HttpGet("metaData")]
    public async Task<ReportMetaData> GetReportMetaData([FromQuery] string reportKey)
        => await _mediator.Send(new GetMetaDataQuery(reportKey, _actingUser));

    [HttpGet("parameter/challenge-specs/{gameId?}")]
    public Task<IEnumerable<SimpleEntity>> GetChallengeSpecs(string gameId = null)
        => _service.ListChallengeSpecs(gameId);

    [HttpGet("parameter/games")]
    public Task<IEnumerable<SimpleEntity>> GetGames()
        => _service.ListGames();

    [HttpGet("parameter/seasons")]
    public Task<IEnumerable<string>> GetSeasons()
        => _service.ListSeasons();

    [HttpGet("parameter/sponsors")]
    public Task<IEnumerable<ReportSponsorViewModel>> GetSponsors()
        => _service.ListSponsors();

    [HttpGet("parameter/series")]
    public Task<IEnumerable<string>> GetSeries()
        => _service.ListSeries();

    [HttpGet("parameter/ticket-statuses")]
    public Task<IEnumerable<string>> GetTicketStatuses()
        => _service.ListTicketStatuses();

    [HttpGet("parameter/tracks")]
    public Task<IEnumerable<string>> GetTracks()
        => _service.ListTracks();
}
