// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Features.Practice;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gameboard.Api.Features.Reports;

[ApiController]
[Authorize]
[Route("/api/reports")]
public class ReportsController(IMediator mediator, IPracticeService practiceService, IReportsService service) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IPracticeService _practiceService = practiceService;
    private readonly IReportsService _service = service;

    [HttpGet]
    public Task<IEnumerable<ReportViewModel>> List()
        => _service.List();

    [HttpGet("challenges")]
    public Task<ReportResults<ChallengesReportStatSummary, ChallengesReportRecord>> GetChallengesReport([FromQuery] ChallengesReportParameters parameters, [FromQuery] PagingArgs paging, CancellationToken cancellationToken)
        => _mediator.Send(new GetChallengesReportQuery(parameters, paging), cancellationToken);

    [HttpGet("enrollment")]
    public Task<ReportResults<EnrollmentReportRecord>> GetEnrollmentReportSummary([FromQuery] EnrollmentReportParameters parameters, [FromQuery] PagingArgs paging, CancellationToken cancellationToken)
        => _mediator.Send(new EnrollmentReportSummaryQuery(parameters, paging), cancellationToken);

    [HttpGet("enrollment/stats")]
    public Task<EnrollmentReportStatSummary> GetEnrollmentReportSummaryStats([FromQuery] EnrollmentReportParameters parameters, CancellationToken cancellationToken)
        => _mediator.Send(new EnrollmentReportSummaryStatsQuery(parameters), cancellationToken);

    [HttpGet("enrollment/trend")]
    public Task<EnrollmentReportLineChartResponse> GetEnrollmentReportLineChart([FromQuery] EnrollmentReportParameters parameters, CancellationToken cancellationToken)
        => _mediator.Send(new EnrollmentReportLineChartQuery(parameters), cancellationToken);

    [HttpGet("enrollment/by-game")]
    public Task<ReportResults<EnrollmentReportByGameRecord>> GetEnrollmentReportByGame([FromQuery] EnrollmentReportParameters parameters, [FromQuery] PagingArgs pagingArgs, CancellationToken cancellationToken)
        => _mediator.Send(new EnrollmentReportByGameQuery(parameters, pagingArgs), cancellationToken);

    [HttpGet("feedback")]
    public Task<ReportResults<FeedbackReportSummaryData, FeedbackReportRecord>> GetFeedbackReport([FromQuery] FeedbackReportParameters request, [FromQuery] PagingArgs pagingArgs, CancellationToken cancellationToken)
        => _mediator.Send(new FeedbackReportQuery(request, pagingArgs), cancellationToken);

    [HttpGet("feedback-legacy")]
    public Task<FeedbackGameReportResults> GetFeedbackGameReport([FromQuery] GetFeedbackGameReportQuery query, CancellationToken cancellationToken)
        => _mediator.Send(query, cancellationToken);

    [HttpGet("players")]
    public Task<ReportResults<PlayersReportStatSummary, PlayersReportRecord>> GetPlayersReport([FromQuery] PlayersReportParameters parameters, [FromQuery] PagingArgs pagingArgs, CancellationToken cancellationToken)
        => _mediator.Send(new GetPlayersReportQuery(parameters, pagingArgs), cancellationToken);

    [HttpGet("practice-area")]
    public async Task<ReportResults<PracticeModeReportOverallStats, IPracticeModeReportRecord>> GetPracticeModeReport([FromQuery] PracticeModeReportParameters parameters, [FromQuery] PagingArgs paging, CancellationToken cancellationToken)
        => await _mediator.Send(new PracticeModeReportQuery(parameters, paging), cancellationToken);

    [HttpGet("practice-area/challenge-spec/{challengeSpecId}")]
    public Task<PracticeModeReportChallengeDetail> GetChallengeDetail([FromQuery] PracticeModeReportParameters parameters, [FromQuery] PracticeModeReportChallengeDetailParameters challengeDetailParameters, [FromQuery] PagingArgs pagingArgs, [FromRoute] string challengeSpecId, CancellationToken cancellationToken)
        => _mediator.Send(new PracticeModeReportChallengeDetailQuery(challengeSpecId, parameters, challengeDetailParameters, pagingArgs), cancellationToken);

    [HttpGet("practice-area/user/{id}/summary")]
    public async Task<PracticeModeReportPlayerModeSummary> GetPracticeModeReportPlayerModeSummary([FromRoute] string id, [FromQuery] bool isPractice, CancellationToken cancellationToken)
        => await _mediator.Send(new PracticeModeReportPlayerModeSummaryQuery(id, isPractice), cancellationToken);

    [HttpGet("site-usage")]
    public Task<SiteUsageReportRecord> GetSiteUsageReport([FromQuery] SiteUsageReportParameters parameters, CancellationToken cancellationToken)
        => _mediator.Send(new GetSiteUsageReportQuery(parameters), cancellationToken);

    [HttpGet("site-usage/challenges")]
    public Task<PagedEnumerable<SiteUsageReportChallengeSpec>> GetSiteUsageReportChallenges([FromQuery] SiteUsageReportParameters reportParameters, [FromQuery] PagingArgs pagingArgs, CancellationToken cancellationToken)
        => _mediator.Send(new GetSiteUsageReportChallengesQuery(reportParameters, pagingArgs), cancellationToken);

    [HttpGet("site-usage/players")]
    public Task<PagedEnumerable<SiteUsageReportPlayer>> GetSiteUsageReportPlayers([FromQuery] SiteUsageReportParameters reportParameters, [FromQuery] SiteUsageReportPlayersParameters playersParameters, [FromQuery] PagingArgs pagingArgs, CancellationToken cancellationToken)
        => _mediator.Send(new GetSiteUsageReportPlayersQuery(reportParameters, playersParameters, pagingArgs), cancellationToken);

    [HttpGet("site-usage/sponsors")]
    public Task<IEnumerable<SiteUsageReportSponsor>> GetSiteUsageReportSponsors([FromQuery] SiteUsageReportParameters reportParameters, CancellationToken cancellationToken)
        => _mediator.Send(new GetSiteUsageReportSponsorsQuery(reportParameters), cancellationToken);

    [HttpGet("support")]
    public Task<ReportResults<SupportReportStatSummary, SupportReportRecord>> GetSupportReport([FromQuery] SupportReportParameters reportParams, [FromQuery] PagingArgs pagingArgs, CancellationToken cancellationToken)
        => _mediator.Send(new SupportReportQuery(reportParams, pagingArgs), cancellationToken);

    [HttpGet("metaData")]
    public Task<ReportMetaData> GetReportMetaData([FromQuery] string reportKey, CancellationToken cancellationToken)
        => _mediator.Send(new GetMetaDataQuery(reportKey), cancellationToken);

    [HttpGet("parameter/challenge-specs/{gameId?}")]
    public Task<IEnumerable<SimpleEntity>> GetChallengeSpecs(string gameId = null)
        => _service.ListChallengeSpecs(gameId);

    [HttpGet("parameter/challenge-tags")]
    public Task<string[]> GetChallengeTags(CancellationToken cancellationToken)
        => _service.ListChallengeTags(cancellationToken);

    [HttpGet("parameter/collections")]
    public Task<SimpleEntity[]> GetCollections(CancellationToken cancellationToken)
        => _mediator.Send(new GetPracticeCollectionsParameterOptionsQuery(), cancellationToken);

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
