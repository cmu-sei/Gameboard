using System.Collections.Generic;
using System.Threading.Tasks;
using Gameboard.Api.Structure;
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

    [HttpGet("players-report")]
    public async Task<PlayersReportResults> GetPlayersReport([FromQuery] PlayersReportQueryParameters reportParams)
        => await _mediator.Send(new GetPlayersReportQuery(reportParams));

    [HttpGet("parameter/challenge-specs/{gameId?}")]
    public Task<IEnumerable<SimpleEntity>> GetChallengeSpecs(string gameId = null)
        => _service.ListParameterOptionsChallengeSpecs(gameId);

    [HttpGet("parameter/competitions")]
    public Task<IEnumerable<string>> GetCompetitions()
        => _service.ListParameterOptionsCompetitions();

    [HttpGet("parameter/games")]
    public Task<IEnumerable<SimpleEntity>> GetGames()
        => _service.ListParameterOptionsGames();

    [HttpGet("parameter/tracks")]
    public Task<IEnumerable<string>> GetTracks()
        => _service.ListParameterOptionsTracks();
}
