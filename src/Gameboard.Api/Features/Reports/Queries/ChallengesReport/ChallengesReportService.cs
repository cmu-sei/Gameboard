// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Features.Practice;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IChallengesReportService
{
    Task<IEnumerable<ChallengesReportRecord>> GetRawResults(ChallengesReportParameters parameters, CancellationToken cancellationToken);
    ChallengesReportStatSummary GetStatSummary(IEnumerable<ChallengesReportRecord> records);
    Task<ChallengeSubmissionCsvRecord[]> GetSubmissionsCsv(string challengeSpecId, ChallengesReportParameters parameters, CancellationToken cancellationToken);
}

internal class ChallengesReportService
(
    IChallengeSubmissionsService challengeSubmissions,
    IPracticeService practiceService,
    IReportsService reportsService,
    IStore store
) : IChallengesReportService
{
    private readonly IChallengeSubmissionsService _challengeSubmissions = challengeSubmissions;
    private readonly IPracticeService _practiceService = practiceService;
    private readonly IReportsService _reportsService = reportsService;
    private readonly IStore _store = store;

    public async Task<IEnumerable<ChallengesReportRecord>> GetRawResults(ChallengesReportParameters parameters, CancellationToken cancellationToken)
    {
        var tagsCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Tags);
        Expression<Func<Data.Challenge, bool>> startDateCondition = c => true;
        Expression<Func<Data.Challenge, bool>> endDateCondition = c => true;

        var query = _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Include(cs => cs.Game)
            .OrderBy(cs => cs.Name)
            .Where(cs => !cs.IsHidden);

        // the date filters apply to challenges, not to specs
        var startDateStart = parameters.StartDateStart.HasValue ? parameters.StartDateStart.Value.ToUniversalTime() : default(DateTimeOffset?);
        var startDateEnd = parameters.StartDateEnd.HasValue ? parameters.StartDateEnd.Value.ToEndDate().ToUniversalTime() : default(DateTimeOffset?);

        if (parameters.StartDateStart.HasValue)
        {
            startDateCondition = c => c.StartTime >= parameters.StartDateStart.Value.ToUniversalTime();
        }

        if (parameters.StartDateEnd.HasValue)
        {
            endDateCondition = c => c.EndTime <= parameters.StartDateEnd.Value.ToEndDate().ToUniversalTime();
        }

        var specs = await GetBaseQuery(parameters)
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
                CompetitiveSolveZeroCount = group
                    .Where(c => c.Score == 0)
                    .Where(c => c.PlayerMode == PlayerMode.Competition)
                    .Count(),
                CompetitiveSolvePartialCount = group
                    .Where(c => c.Score > 0 && c.Score < c.Points)
                    .Where(c => c.PlayerMode == PlayerMode.Competition)
                    .Count(),
                CompetitiveSolveCompleteCount = group
                    .Where(c => c.Score >= c.Points)
                    .Where(c => c.PlayerMode == PlayerMode.Competition)
                    .Count(),
                PracticeSolveZeroCount = group
                    .Where(c => c.Score == 0)
                    .Where(c => c.PlayerMode == PlayerMode.Practice)
                    .Count(),
                PracticeSolvePartialCount = group
                    .Where(c => c.Score > 0 && c.Score < c.Points)
                    .Where(c => c.PlayerMode == PlayerMode.Practice)
                    .Count(),
                PracticeSolveCompleteCount = group
                    .Where(c => c.Score >= c.Points)
                    .Where(c => c.PlayerMode == PlayerMode.Practice)
                    .Count(),
            }, cancellationToken);

        // we currently restrict tags we show on challenges (to avoid polluting the UI with internal tags).
        // the non-awesome part of this is that we do it using the practice settings, because that's where we needed it first
        var visibleTags = await _practiceService.GetVisibleChallengeTags(specs.SelectMany(s => s.Tags), cancellationToken);

        var preSortResults = specs.Select(cs =>
        {
            var aggregations = specAggregations.ContainsKey(cs.Id) ? specAggregations[cs.Id] : null;
            var tags = (cs.Tags.IsEmpty() ? [] : cs.Tags).Where(visibleTags.Contains).ToArray();

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
                Tags = tags,
                AvgCompleteSolveTimeMs = aggregations?.AvgCompleteSolveTimeMs,
                AvgScore = aggregations?.AvgScore,
                DeployCompetitiveCount = aggregations?.DeployCompetitiveCount ?? 0,
                DeployPracticeCount = aggregations?.DeployPracticeCount ?? 0,
                DistinctPlayerCount = aggregations?.DistinctPlayerCount ?? 0,
                PracticeSolveZeroCount = aggregations?.PracticeSolveZeroCount ?? 0,
                PracticeSolvePartialCount = aggregations?.PracticeSolvePartialCount ?? 0,
                PracticeSolveCompleteCount = aggregations?.PracticeSolveCompleteCount ?? 0,
                SolveZeroCount = aggregations?.CompetitiveSolveZeroCount ?? 0,
                SolvePartialCount = aggregations?.CompetitiveSolvePartialCount ?? 0,
                SolveCompleteCount = aggregations?.CompetitiveSolveCompleteCount ?? 0
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

    public async Task<ChallengeSubmissionCsvRecord[]> GetSubmissionsCsv(string challengeSpecId, ChallengesReportParameters parameters, CancellationToken cancellationToken)
    {
        var query = GetBaseQuery(parameters);

        if (challengeSpecId.IsNotEmpty())
        {
            query = query.Where(s => s.Id == challengeSpecId);
        }

        var challengeSpecs = await query.Select(s => s.Id).ToArrayAsync(cancellationToken);
        var challengesQuery = _store.WithNoTracking<Data.Challenge>().Where(c => challengeSpecs.Contains(c.SpecId));
        return await _challengeSubmissions.GetSubmissionsCsv(challengesQuery, cancellationToken);
    }

    private IQueryable<Data.ChallengeSpec> GetBaseQuery(ChallengesReportParameters parameters)
    {
        var gamesCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Games);
        var seasonsCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Seasons);
        var seriesCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Series);
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
        var startDateStart = parameters.StartDateStart.HasValue ? parameters.StartDateStart.Value.ToUniversalTime() : default(DateTimeOffset?);
        var startDateEnd = parameters.StartDateEnd.HasValue ? parameters.StartDateEnd.Value.ToEndDate().ToUniversalTime() : default(DateTimeOffset?);
        Expression<Func<Data.Challenge, bool>> startDateCondition = c => true;
        Expression<Func<Data.Challenge, bool>> endDateCondition = c => true;

        if (parameters.StartDateStart.HasValue)
            startDateCondition = c => c.StartTime >= parameters.StartDateStart.Value.ToUniversalTime();

        if (parameters.StartDateEnd.HasValue)
            endDateCondition = c => c.EndTime <= parameters.StartDateEnd.Value.ToEndDate().ToUniversalTime();

        return query;
    }
}
