using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Common;
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

    [HttpGet("challenges-report")]
    public Task<ReportResults<ChallengesReportRecord>> GetChallengeReport([FromQuery] GetChallengesReportQueryArgs args)
        => _mediator.Send(new ChallengesReportQuery(args));

    [HttpGet("enrollment")]
    public Task<ReportResults<EnrollmentReportRecord>> GetEnrollmentReport([FromQuery] EnrollmentReportParameters parameters, [FromQuery] PagingArgs paging)
        => _mediator.Send(new EnrollmentReportQuery(parameters, paging));

    [HttpGet("players-report")]
    public async Task<ReportResults<PlayersReportRecord>> GetPlayersReport([FromQuery] PlayersReportQueryParameters reportParams)
        => await _mediator.Send(new PlayersReportQuery(reportParams));

    [HttpGet("support-report")]
    public async Task<ReportResults<SupportReportRecord>> GetSupportReport([FromQuery] SupportReportParameters reportParams)
        => await _mediator.Send(new SupportReportQuery(reportParams));

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
    public Task<IEnumerable<SimpleEntity>> GetSponsors()
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
