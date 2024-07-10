using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Features.Challenges;
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

    public ReportsController
    (
        IMediator mediator,
        IReportsService service
    )
    {
        _mediator = mediator;
        _service = service;
    }

    [HttpGet]
    public async Task<IEnumerable<ReportViewModel>> List()
        => await _service.List();

    [HttpGet("challenges")]
    public Task<ReportResults<ChallengesReportStatSummary, ChallengesReportRecord>> GetChallengesReport([FromQuery] ChallengesReportParameters parameters, [FromQuery] PagingArgs paging)
        => _mediator.Send(new GetChallengesReportQuery(parameters, paging));

    [HttpGet("enrollment")]
    public Task<ReportResults<EnrollmentReportRecord>> GetEnrollmentReportSummary([FromQuery] EnrollmentReportParameters parameters, [FromQuery] PagingArgs paging)
        => _mediator.Send(new EnrollmentReportSummaryQuery(parameters, paging));

    [HttpGet("enrollment/stats")]
    public Task<EnrollmentReportStatSummary> GetEnrollmentReportSummaryStats([FromQuery] EnrollmentReportParameters parameters)
        => _mediator.Send(new EnrollmentReportSummaryStatsQuery(parameters));

    [HttpGet("enrollment/trend")]
    public Task<IDictionary<DateTimeOffset, EnrollmentReportLineChartGroup>> GetEnrollmentReportLineChart([FromQuery] EnrollmentReportParameters parameters)
        => _mediator.Send(new EnrollmentReportLineChartQuery(parameters));

    [HttpGet("enrollment/by-game")]
    public Task<ReportResults<EnrollmentReportByGameRecord>> GetEnrollmentReportByGame([FromQuery] EnrollmentReportParameters parameters, [FromQuery] PagingArgs pagingArgs)
        => _mediator.Send(new EnrollmentReportByGameQuery(parameters, pagingArgs));

    [HttpGet("players")]
    public Task<ReportResults<PlayersReportStatSummary, PlayersReportRecord>> GetPlayersReport([FromQuery] PlayersReportParameters parameters, [FromQuery] PagingArgs pagingArgs)
        => _mediator.Send(new GetPlayersReportQuery(parameters, pagingArgs));

    [HttpGet("practice-area")]
    public async Task<ReportResults<PracticeModeReportOverallStats, IPracticeModeReportRecord>> GetPracticeModeReport([FromQuery] PracticeModeReportParameters parameters, [FromQuery] PagingArgs paging)
        => await _mediator.Send(new PracticeModeReportQuery(parameters, paging));

    [HttpGet("practice-area/challenge-spec/{challengeSpecId}")]
    public Task<PracticeModeReportChallengeDetail> GetChallengeDetail([FromQuery] PracticeModeReportParameters parameters, [FromQuery] PracticeModeReportChallengeDetailParameters challengeDetailParameters, [FromQuery] PagingArgs pagingArgs, [FromRoute] string challengeSpecId)
        => _mediator.Send(new PracticeModeReportChallengeDetailQuery(challengeSpecId, parameters, challengeDetailParameters, pagingArgs));

    [HttpGet("practice-area/user/{id}/summary")]
    public async Task<PracticeModeReportPlayerModeSummary> GetPracticeModeReportPlayerModeSummary([FromRoute] string id, [FromQuery] bool isPractice)
        => await _mediator.Send(new PracticeModeReportPlayerModeSummaryQuery(id, isPractice));

    [HttpGet("site-usage")]
    public Task<SiteUsageReportRecord> GetSiteUsageReport([FromQuery] SiteUsageReportParameters parameters)
        => _mediator.Send(new GetSiteUsageReportQuery(parameters));

    [HttpGet("site-usage/challenges")]
    public Task<PagedEnumerable<SiteUsageReportChallenge>> GetSiteUsageREportChallenges([FromQuery] SiteUsageReportParameters reportParameters, [FromQuery] PagingArgs pagingArgs)
        => _mediator.Send(new GetSiteUsageReportChallengesQuery(reportParameters, pagingArgs));

    [HttpGet("site-usage/players")]
    public Task<PagedEnumerable<SiteUsageReportPlayer>> GetSiteUsageReportPlayers([FromQuery] SiteUsageReportParameters reportParameters, [FromQuery] SiteUsageReportPlayersParameters playersParameters, [FromQuery] PagingArgs pagingArgs)
        => _mediator.Send(new GetSiteUsageReportPlayersQuery(reportParameters, playersParameters, pagingArgs));

    [HttpGet("site-usage/sponsors")]
    public Task<IEnumerable<SiteUsageReportSponsor>> GetSiteUsageReportSponsors([FromQuery] SiteUsageReportParameters reportParameters)
        => _mediator.Send(new GetSiteUsageReportSponsorsQuery(reportParameters));

    [HttpGet("support")]
    public Task<ReportResults<SupportReportStatSummary, SupportReportRecord>> GetSupportReport([FromQuery] SupportReportParameters reportParams, [FromQuery] PagingArgs pagingArgs)
        => _mediator.Send(new SupportReportQuery(reportParams, pagingArgs));

    [HttpGet("metaData")]
    public Task<ReportMetaData> GetReportMetaData([FromQuery] string reportKey)
        => _mediator.Send(new GetMetaDataQuery(reportKey));

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
