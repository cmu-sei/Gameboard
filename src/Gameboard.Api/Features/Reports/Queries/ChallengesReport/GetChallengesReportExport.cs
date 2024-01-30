using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Challenges;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record GetChallengesReportExportQuery(ChallengesReportParameters Parameters, User ActingUser) : IRequest<IEnumerable<ChallengesReportExportRecord>>, IReportQuery;

internal class GetChallengesReportExportHandler : IRequestHandler<GetChallengesReportExportQuery, IEnumerable<ChallengesReportExportRecord>>
{
    private readonly IChallengesReportService _challengesReportService;
    private readonly ReportsQueryValidator _reportsQueryValidator;

    public GetChallengesReportExportHandler
    (
        IChallengesReportService challengesReportService,
        ReportsQueryValidator validator
    )
    {
        _challengesReportService = challengesReportService;
        _reportsQueryValidator = validator;
    }

    public async Task<IEnumerable<ChallengesReportExportRecord>> Handle(GetChallengesReportExportQuery request, CancellationToken cancellationToken)
    {
        await _reportsQueryValidator.Validate(request, cancellationToken);

        var rawResults = await _challengesReportService.GetRawResults(request.Parameters, cancellationToken);
        return rawResults.Select(r => new ChallengesReportExportRecord
        {
            ChallengeSpecId = r.ChallengeSpec.Id,
            ChallengeSpecName = r.ChallengeSpec.Name,
            GameId = r.Game.Id,
            GameName = r.Game.Name,
            GameSeason = r.Game.Season,
            GameSeries = r.Game.Series,
            GameTrack = r.Game.Track,
            CurrentPlayerMode = r.PlayerModeCurrent,
            Points = r.Points,
            GameEngineTags = string.Join(",", r.Tags),
            AttemptCount = r.AttemptCount,
            AvgCompleteSolveTimeMs = r.AvgCompleteSolveTimeMs,
            AvgCompleteSolveTime = r.AvgCompleteSolveTimeMs is not null ? TimeSpan.FromMilliseconds(r.AvgCompleteSolveTimeMs.Value).ToString("g") : null,
            AvgScore = r.AvgScore,
            DeployCompetitiveCount = r.DeployCompetitiveCount,
            DeployPracticeCount = r.DeployPracticeCount,
            DistinctPlayerCount = r.DistinctPlayerCount,
            SolveZeroCount = r.SolveZeroCount,
            SolvePartialCount = r.SolvePartialCount,
            SolveCompleteCount = r.SolveCompleteCount,
            SolveZeroPct = r.AttemptCount > 0 ? (r.SolveZeroCount / r.AttemptCount) : null,
            SolvePartialPct = r.AttemptCount > 0 ? (r.SolvePartialCount / r.AttemptCount) : null,
            SolveCompletePct = r.AttemptCount > 0 ? (r.SolveCompleteCount / r.AttemptCount) : null
        });
    }
}
