using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Challenges;
using Microsoft.EntityFrameworkCore;
using ServiceStack;

namespace Gameboard.Api.Features.Reports;

public interface IChallengesReportService
{
    Task<IEnumerable<ChallengesReportRecord>> GetRawResults(ChallengesReportParameters parameters, CancellationToken cancellationToken);
    ChallengesReportStatSummary GetStatSummary(IEnumerable<ChallengesReportRecord> records);
}

internal class ChallengesReportService(IReportsService reportsService, IStore store) : IChallengesReportService
{
    private readonly IReportsService _reportsService = reportsService;
    private readonly IStore _store = store;

    public async Task<IEnumerable<ChallengesReportRecord>> GetRawResults(ChallengesReportParameters parameters, CancellationToken cancellationToken)
    {
        var gamesCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Games);
        var seasonsCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Seasons);
        var seriesCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Series);
        var tagsCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Tags);
        var tracksCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Tracks);

        var query = _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Include(cs => cs.Game)
            .OrderBy(cs => cs.Name)
            .Where(cs => !cs.IsHidden);

        if (gamesCriteria.Any())
            query = query.Where(cs => gamesCriteria.Contains(cs.GameId));

        if (seasonsCriteria.Any())
            query = query.Where(cs => seasonsCriteria.Contains(cs.Game.Season.ToLower()));

        if (seriesCriteria.Any())
            query = query.Where(cs => seriesCriteria.Contains(cs.Game.Competition.ToLower()));

        if (tracksCriteria.Any())
            query = query.Where(cs => tracksCriteria.Contains(cs.Game.Track.ToLower()));

        // the date filters apply to challenges, not to specs
        DateTimeOffset? startDateStart = parameters.StartDateStart.HasValue ? parameters.StartDateStart.Value.ToUniversalTime() : null;
        DateTimeOffset? startDateEnd = parameters.StartDateEnd.HasValue ? parameters.StartDateEnd.Value.ToEndDate().ToUniversalTime() : null;
        Expression<Func<Data.Challenge, bool>> startDateCondition = c => true;
        Expression<Func<Data.Challenge, bool>> endDateCondition = c => true;

        if (parameters.StartDateStart.HasValue)
            startDateCondition = c => c.StartTime >= parameters.StartDateStart.Value.ToUniversalTime();

        if (parameters.StartDateEnd.HasValue)
            endDateCondition = c => c.EndTime <= parameters.StartDateEnd.Value.ToEndDate().ToUniversalTime();

        var specs = await query
            .Select(cs => new
            {
                cs.Id,
                cs.Name,
                cs.GameId,
                GameName = cs.Game.Name,
                cs.Game.Season,
                cs.Game.Competition,
                cs.Game.Track,
                cs.Game.MaxTeamSize,
                PlayerModeCurrent = cs.Game.PlayerMode,
                cs.Points,
                Tags = cs.Tags != null ? cs.Tags.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) : Array.Empty<string>()
            })
            .ToArrayAsync(cancellationToken);

        // the tags are really messy to evaluate server-side because they're not relational, so do here for now
        if (tagsCriteria.Any())
        {
            specs = specs.Where(s => tagsCriteria.Intersect(s.Tags).Any()).ToArray();
        }

        var specIds = specs.Select(cs => cs.Id).ToArray();

        var specAggregations = await _store
            .WithNoTracking<Data.Challenge>()
                .Include(c => c.Player)
            .AsSplitQuery()
            .Where(c => specIds.Contains(c.SpecId))
            .Where(startDateCondition)
            .Where(endDateCondition)
            .Select(c => new
            {
                c.Id,
                c.SpecId,
                c.Points,
                c.Score,
                c.Duration,
                c.Player,
                c.PlayerMode
            })
            .GroupBy(c => c.SpecId)
            .ToDictionaryAsync(group => group.Key, group => new
            {
                AvgScore = group.Any() ? group.Average(c => c.Score) as double? : null,
                AvgCompleteSolveTimeMs = group.Where(c => c.Score >= c.Points).Any() ? group
                    .Where(c => c.Score >= c.Points)
                    .Average(c => c.Duration) as double? :
                    null,
                DeployCompetitiveCount = group
                    .Where(c => c.PlayerMode == PlayerMode.Competition)
                    .Count(),
                DeployPracticeCount = group
                    .Where(c => c.PlayerMode == PlayerMode.Practice)
                    .Count(),
                DistinctPlayerCount = group
                    .Select(c => c.Player)
                    .Select(p => p.UserId)
                    .Distinct()
                    .Count(),
                SolveZeroCount = group
                    .Where(c => c.Score == 0)
                    .Where(c => c.PlayerMode == PlayerMode.Competition)
                    .Count(),
                SolvePartialCount = group
                    .Where(c => c.Score > 0 && c.Score < c.Points)
                    .Where(c => c.PlayerMode == PlayerMode.Competition)
                    .Count(),
                SolveCompleteCount = group
                    .Where(c => c.Score >= c.Points)
                    .Where(c => c.PlayerMode == PlayerMode.Competition)
                    .Count()
            }, cancellationToken);

        var preSortResults = specs.Select(cs =>
        {
            var aggregations = specAggregations.ContainsKey(cs.Id) ? specAggregations[cs.Id] : null;

            return new ChallengesReportRecord
            {
                ChallengeSpec = new SimpleEntity { Id = cs.Id, Name = cs.Name },
                Game = new ReportGameViewModel
                {
                    Id = cs.GameId,
                    Name = cs.GameName,
                    Season = cs.Season,
                    Series = cs.Competition,
                    Track = cs.Track,
                    IsTeamGame = cs.MaxTeamSize > 1
                },
                PlayerModeCurrent = cs.PlayerModeCurrent,
                Points = cs.Points,
                Tags = cs.Tags.IsNotEmpty() ? cs.Tags : [],
                AvgCompleteSolveTimeMs = aggregations?.AvgCompleteSolveTimeMs,
                AvgScore = aggregations?.AvgScore,
                DeployCompetitiveCount = aggregations is not null ? aggregations.DeployCompetitiveCount : 0,
                DeployPracticeCount = aggregations is not null ? aggregations.DeployPracticeCount : 0,
                DistinctPlayerCount = aggregations is not null ? aggregations.DistinctPlayerCount : 0,
                SolveZeroCount = aggregations is not null ? aggregations.SolveZeroCount : 0,
                SolvePartialCount = aggregations is not null ? aggregations.SolvePartialCount : 0,
                SolveCompleteCount = aggregations is not null ? aggregations.SolveCompleteCount : 0
            };
        });

        var sortedResults = preSortResults.OrderBy(r => true);

        if (parameters.Sort.IsNotEmpty())
        {
            switch (parameters.Sort.ToLower())
            {
                case "avg-score":
                    sortedResults = sortedResults.Sort(r => r.AvgScore, parameters.SortDirection);
                    break;
                case "avg-solve-time":
                    sortedResults = sortedResults.Sort(r => r.AvgCompleteSolveTimeMs, parameters.SortDirection);
                    break;
                case "deploy-count-competitive":
                    sortedResults = sortedResults.Sort(r => r.DeployCompetitiveCount, parameters.SortDirection);
                    break;
                case "deploy-count-practice":
                    sortedResults = sortedResults.Sort(r => r.DeployPracticeCount, parameters.SortDirection);
                    break;
                case "player-count":
                    sortedResults = sortedResults.Sort(r => r.DistinctPlayerCount, parameters.SortDirection);
                    break;
                case "score-complete":
                    sortedResults = sortedResults.Sort(r => r.SolveCompleteCount, parameters.SortDirection);
                    break;
                case "score-none":
                    sortedResults = sortedResults.Sort(r => r.SolveZeroCount, parameters.SortDirection);
                    break;
                case "score-partial":
                    sortedResults = sortedResults.Sort(r => r.SolvePartialCount, parameters.SortDirection);
                    break;
            }
        }

        return sortedResults
            .ThenBy(r => r.ChallengeSpec.Name)
            .ThenBy(r => r.Game.Name);
    }

    public ChallengesReportStatSummary GetStatSummary(IEnumerable<ChallengesReportRecord> records)
    {
        var specIds = records.Select(r => r.ChallengeSpec.Id).ToArray();
        var mostPopularCompetitiveChallengeSpec = records
            .Where(r => r.DeployCompetitiveCount > 0)
            .OrderByDescending(r => r.DeployCompetitiveCount)
            .FirstOrDefault();

        var mostPopularPracticeChallengeSpec = records
            .Where(r => r.DeployPracticeCount > 0)
            .OrderByDescending(r => r.DeployPracticeCount)
            .FirstOrDefault();

        return new ChallengesReportStatSummary
        {
            DeployCompetitiveCount = records.Sum(r => r.DeployCompetitiveCount),
            DeployPracticeCount = records.Sum(r => r.DeployPracticeCount),
            SpecCount = records.Count(),
            MostPopularCompetitiveChallenge = mostPopularCompetitiveChallengeSpec is null ?
                null :
                new ChallengesReportStatSummaryPopularChallenge
                {
                    ChallengeName = mostPopularCompetitiveChallengeSpec.ChallengeSpec.Name,
                    GameName = mostPopularCompetitiveChallengeSpec.Game.Name,
                    DeployCount = mostPopularCompetitiveChallengeSpec.DeployCompetitiveCount
                },
            MostPopularPracticeChallenge = mostPopularPracticeChallengeSpec is null ?
                null :
                new ChallengesReportStatSummaryPopularChallenge
                {
                    ChallengeName = mostPopularPracticeChallengeSpec.ChallengeSpec.Name,
                    GameName = mostPopularPracticeChallengeSpec.Game.Name,
                    DeployCount = mostPopularPracticeChallengeSpec.DeployPracticeCount
                }
        };
    }
}
