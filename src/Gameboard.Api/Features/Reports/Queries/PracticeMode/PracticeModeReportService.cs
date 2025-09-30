// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IPracticeModeReportService
{
    IQueryable<Data.Challenge> GetBaseQuery(PracticeModeReportParameters parameters, bool includeCompetitive);
    Task<IEnumerable<PracticeModeReportCsvRecord>> GetCsvExport(PracticeModeReportParameters parameters, CancellationToken cancellationToken);
    Task<PracticeModeReportPlayerModeSummary> GetPlayerModePerformanceSummary(string userId, bool isPractice, CancellationToken cancellationToken);
    Task<PracticeModeReportResults> GetResultsByChallenge(PracticeModeReportParameters parameters, CancellationToken cancellationToken);
    Task<PracticeModeReportResults> GetResultsByUser(PracticeModeReportParameters parameters, CancellationToken cancellationToken);
    Task<PracticeModeReportResults> GetResultsByPlayerModePerformance(PracticeModeReportParameters parameters, CancellationToken cancellationToken);
    Task<ChallengeSubmissionCsvRecord[]> GetSubmissionsCsv(string challengeSpecId, PracticeModeReportParameters parameters, CancellationToken cancellationToken);
}

internal class PracticeModeReportService
(
    ChallengeService challengeService,
    IChallengeSubmissionsService challengeSubmissionsService,
    IPracticeService practiceService,
    IReportsService reportsService,
    IStore store
) : IPracticeModeReportService
{
    private readonly ChallengeService _challengeService = challengeService;
    private readonly IChallengeSubmissionsService _challengeSubmissionsService = challengeSubmissionsService;
    private readonly IPracticeService _practiceService = practiceService;
    private readonly IReportsService _reportsService = reportsService;
    private readonly IStore _store = store;

    private sealed class PracticeModeReportUngroupedResults
    {
        public required PracticeModeReportOverallStats OverallStats { get; set; }
        public required IEnumerable<Data.Challenge> Challenges { get; set; }
        public required IDictionary<string, Data.ChallengeSpec> Specs { get; set; }
        public required IEnumerable<ReportSponsorViewModel> Sponsors { get; set; }
    }

    private async Task<PracticeModeReportUngroupedResults> BuildUngroupedResults(PracticeModeReportParameters parameters, bool includeCompetitive, CancellationToken cancellationToken)
    {
        var query = GetBaseQuery(parameters, includeCompetitive);

        // query for the raw results
        var challenges = await query
            .Include(c => c.Spec)
                .ThenInclude(s => s.Game)
            .Select(c => new
            {
                Challenge = c,
                c.Spec,
                Sponsor = c.Player.Sponsor.ToReportViewModel(),
                c.Player.UserId,
            })
            .ToArrayAsync(cancellationToken);

        return new PracticeModeReportUngroupedResults
        {
            Challenges = [.. challenges.Select(c => c.Challenge)],
            OverallStats = new()
            {
                AttemptCount = challenges.Length,
                ChallengeCount = challenges
                    .Select(c => c.Spec.Id)
                    .Distinct()
                    .Count(),
                CompletionCount = challenges
                    .Where(c => c.Challenge.Score >= c.Challenge.Points)
                    .Count(),
                PlayerCount = challenges
                    .Select(c => c.UserId)
                    .Distinct()
                    .Count(),
                SponsorCount = challenges
                    .Select(c => c.Sponsor.Id)
                    .Distinct()
                    .Count()
            },
            Specs = challenges.Select(c => c.Spec).DistinctBy(s => s.Id).ToDictionary(s => s.Id, s => s),
            Sponsors = challenges.Select(c => c.Sponsor).DistinctBy(s => s.Id) ?? []
        };
    }

    private PracticeModeReportByChallengePerformance BuildChallengePerformance(IEnumerable<Data.Challenge> attempts, ReportSponsorViewModel sponsor = null)
    {
        // precompute totals so we don't have to do it more than once per spec
        Func<Data.Challenge, bool> sponsorConstraint = attempts => true;
        if (sponsor is not null)
            sponsorConstraint = (attempt) => attempt.Player.SponsorId == sponsor.Id;

        var totalAttempts = attempts.Where(sponsorConstraint).Count();
        var completeSolves = attempts
            .Where(sponsorConstraint)
            .Where(a => a.Result == ChallengeResult.Success).Count();
        var partialSolves = attempts
            .Where(sponsorConstraint)
            .Where(a => a.Result == ChallengeResult.Partial).Count();
        var zeroScoreSolves = attempts
            .Where(sponsorConstraint)
            .Where(a => a.Result == ChallengeResult.None).Count();

        return new PracticeModeReportByChallengePerformance
        {
            Players = (sponsor is null ? attempts : attempts.Where(sponsorConstraint)).
                Select(a => new SimpleEntity { Id = a.Player.UserId, Name = a.Player.ApprovedName })
                .DistinctBy(e => e.Id)
                .ToArray(),
            TotalAttempts = totalAttempts,
            CompleteSolves = completeSolves,
            ScoreHigh = new decimal(attempts.Select(a => a.Score).Max()),
            ScoreAvg = new decimal(attempts.Select(a => a.Score).Average()),
            PercentageCompleteSolved = totalAttempts > 0 ? decimal.Divide(completeSolves, totalAttempts) : null,
            PartialSolves = partialSolves,
            PercentagePartiallySolved = totalAttempts > 0 ? decimal.Divide(partialSolves, totalAttempts) : null,
            ZeroScoreSolves = zeroScoreSolves,
            PercentageZeroScoreSolved = totalAttempts > 0 ? decimal.Divide(zeroScoreSolves, totalAttempts) : null
        };
    }

    public IQueryable<Data.Challenge> GetBaseQuery(PracticeModeReportParameters parameters, bool includeCompetitive)
    {
        // process parameters
        DateTimeOffset? startDate = parameters.PracticeDateStart.HasValue ? parameters.PracticeDateStart.Value.ToUniversalTime() : null;
        DateTimeOffset? endDate = parameters.PracticeDateEnd.HasValue ? parameters.PracticeDateEnd.Value.ToEndDate().ToUniversalTime() : null;
        var collectionIds = _reportsService.ParseMultiSelectCriteria(parameters.Collections);
        var gameIds = _reportsService.ParseMultiSelectCriteria(parameters.Games);
        var sponsorIds = _reportsService.ParseMultiSelectCriteria(parameters.Sponsors);
        var seasons = _reportsService.ParseMultiSelectCriteria(parameters.Seasons);
        var series = _reportsService.ParseMultiSelectCriteria(parameters.Series);
        var tracks = _reportsService.ParseMultiSelectCriteria(parameters.Tracks);

        var query = _store
            .WithNoTracking<Data.Challenge>()
                .AsSplitQuery()
                .Include(c => c.Game)
                .Include(c => c.Player)
                    .ThenInclude(p => p.Sponsor)
                .Include(c => c.Player)
                    .ThenInclude(p => p.User)
            .Where(c => c.PlayerMode == PlayerMode.Practice || includeCompetitive);

        if (startDate is not null)
        {
            query = query
                .WhereDateIsNotEmpty(c => c.StartTime)
                .Where(c => c.StartTime >= startDate);
        }

        if (endDate is not null)
        {
            query = query
                .WhereDateIsNotEmpty(c => c.EndTime)
                .Where(c => c.EndTime <= endDate);
        }

        if (collectionIds.IsNotEmpty())
            query = query.Where(c => c.Spec.PracticeChallengeGroups.Any(g => collectionIds.Contains(g.PracticeChallengeGroupId)));

        if (seasons.IsNotEmpty())
            query = query.Where(c => seasons.Contains(c.Game.Season.ToLower()));

        if (series.IsNotEmpty())
            query = query.Where(c => series.Contains(c.Game.Competition.ToLower()));

        if (tracks.IsNotEmpty())
            query = query.Where(c => tracks.Contains(c.Game.Track.ToLower()));

        if (gameIds.IsNotEmpty())
            query = query.Where(c => gameIds.Contains(c.GameId));

        if (sponsorIds.IsNotEmpty())
            query = query.Where(c => sponsorIds.Contains(c.Player.Sponsor.Id));

        return query;
    }

    public async Task<PracticeModeReportResults> GetResultsByChallenge(PracticeModeReportParameters parameters, CancellationToken cancellationToken)
    {
        // the "false" argument here excludes competitive records (this grouping only looks at practice challenges)
        var ungroupedResults = await BuildUngroupedResults(parameters, false, cancellationToken);

        // get tags that we want to display
        var visibleTags = await _practiceService.GetVisibleChallengeTags(cancellationToken);

        var records = ungroupedResults
            .Challenges
            .GroupBy(c => c.SpecId)
            .Select(g =>
            {
                var attempts = g.ToList();
                var spec = ungroupedResults.Specs[g.Key];
                var specTags = _challengeService.GetTags(spec.Tags);

                var sponsorIdsPlayed = attempts.Select(a => a.Player.Sponsor.Id).Distinct();
                var sponsorsPlayed = ungroupedResults
                    .Sponsors
                    .Where(s => sponsorIdsPlayed.Contains(s.Id));

                // overall results across all sponsors
                var performanceOverall = BuildChallengePerformance(attempts);

                // performance by sponsor
                var performanceBySponsor = sponsorsPlayed.Select(s => new PracticeModeReportByChallengePerformanceBySponsor
                {
                    Sponsor = s,
                    Performance = BuildChallengePerformance(attempts, s)
                });

                return new PracticeModeByChallengeReportRecord
                {
                    Id = spec.Id,
                    Name = spec.Name,
                    Game = new ReportGameViewModel
                    {
                        Id = spec.GameId,
                        Name = spec.Game.Name,
                        IsTeamGame = spec.Game.IsTeamGame(),
                        Series = spec.Game.Competition,
                        Season = spec.Game.Season,
                        Track = spec.Game.Track
                    },
                    MaxPossibleScore = spec.Points,
                    AvgScore = attempts.Select(a => a.Score).Average(),
                    Description = spec.Description,
                    Tags = specTags.Intersect(visibleTags).ToArray(),
                    Text = spec.Text,
                    SponsorsPlayed = sponsorsPlayed,
                    OverallPerformance = performanceOverall,
                    PerformanceBySponsor = performanceBySponsor
                };
            })
            .OrderBy(c => c.Name);

        if (parameters.Sort.IsNotEmpty())
        {
            switch (parameters.Sort.ToLower())
            {
                case "attempts":
                    records = records.Sort(r => r.OverallPerformance.TotalAttempts, parameters.SortDirection);
                    break;
                case "count-solve-complete":
                    records = records.Sort(r => r.OverallPerformance.CompleteSolves, parameters.SortDirection);
                    break;
                case "count-solve-none":
                    records = records.Sort(r => r.OverallPerformance.ZeroScoreSolves, parameters.SortDirection);
                    break;
                case "count-solve-partial":
                    records = records.Sort(r => r.OverallPerformance.PartialSolves, parameters.SortDirection);
                    break;
                case "count-sponsors":
                    records = records.Sort(r => r.SponsorsPlayed.Count(), parameters.SortDirection);
                    break;
                case "name":
                    records = records.Sort(r => r.Name, parameters.SortDirection);
                    break;
                case "players":
                    records = records.Sort(r => r.OverallPerformance.Players.Count(), parameters.SortDirection);
                    break;
                case "score-avg":
                    records = records.Sort(r => r.OverallPerformance.ScoreAvg, parameters.SortDirection);
                    break;
                case "score-high":
                    records = records.Sort(r => r.OverallPerformance.ScoreHigh, parameters.SortDirection);
                    break;
                case "score-max":
                    records = records.Sort(r => r.MaxPossibleScore, parameters.SortDirection);
                    break;
            }
        }

        records = records.ThenBy(r => r.Name);

        return new()
        {
            OverallStats = ungroupedResults.OverallStats,
            Records = records
        };
    }

    public async Task<PracticeModeReportResults> GetResultsByUser(PracticeModeReportParameters parameters, CancellationToken cancellationToken)
    {
        // the "false" argument here excludes competitive records (this grouping only looks at practice challenges)
        var ungroupedResults = await BuildUngroupedResults(parameters, false, cancellationToken);

        // resolve userIds, teams, and challengeIds
        var challengeIds = ungroupedResults.Challenges.Select(c => c.Id);
        var userIds = ungroupedResults.Challenges.Select(c => c.Player.UserId);
        var teams = await _reportsService.GetTeamsByPlayerIds(ungroupedResults.Challenges.Select(c => c.PlayerId), cancellationToken);

        // for the group-by-players version of the report, we group on player and challenge spec,
        // and we also need each user's competitive performance for reporting
        var groupByPlayerAndChallengeSpec = ungroupedResults.Challenges.GroupBy(d => new { d.Player.UserId, d.SpecId });

        // alias the specs dictionary for convenience later
        var specs = ungroupedResults.Specs;

        // screen out invisible tags 
        var visibleTags = await _practiceService.GetVisibleChallengeTags(cancellationToken);

        // translate to records
        var records = groupByPlayerAndChallengeSpec.Select(c => new PracticeModeByUserReportRecord
        {
            User = new PracticeModeReportUser
            {
                Id = c.Key.UserId,
                Name = c.Select(c => c.Player.ApprovedName).First(),
                // report their sponsor as the most recent attempt that has a sponsor
                // (which they all should, but still)
                Sponsor = ungroupedResults.Sponsors
                    .FirstOrDefault
                    (
                        s => s.Id == c
                            .OrderByDescending(c => c.StartTime)
                            .FirstOrDefault(c => c.Player.Sponsor is not null)?.Player?.Sponsor?.Id
                    ),
                HasScoringAttempt = c.Any(c => c.Score > 0)
            },
            Challenge = new PracticeModeByUserReportChallenge
            {
                Id = c.Key.SpecId,
                Name = specs[c.Key.SpecId].Name,
                Game = new ReportGameViewModel
                {
                    Id = specs[c.Key.SpecId].Game.Id,
                    Name = specs[c.Key.SpecId].Game.Name,
                    IsTeamGame = specs[c.Key.SpecId].Game.IsTeamGame(),
                    Series = specs[c.Key.SpecId].Game.Competition,
                    Season = specs[c.Key.SpecId].Game.Season,
                    Track = specs[c.Key.SpecId].Game.Track
                },
                MaxPossibleScore = specs[c.Key.SpecId].Points,
                Tags = visibleTags.Intersect(_challengeService.GetTags(specs[c.Key.SpecId].Tags))
            },
            Attempts = c
                .ToList()
                .Select(attempt => new PracticeModeReportAttempt
                {
                    Player = new SimpleEntity { Id = attempt.PlayerId, Name = attempt.Player.ApprovedName },
                    Team = teams.TryGetValue(attempt.Player.TeamId, out ReportTeamViewModel value) ? value : null,
                    Sponsor = ungroupedResults.Sponsors.FirstOrDefault(s => s.Id == attempt.Player.Sponsor.Id),
                    Start = attempt.StartTime,
                    End = attempt.EndTime,
                    DurationMs = attempt.Player.Time,
                    Result = attempt.Result,
                    Score = attempt.Score,
                    PartiallyCorrectCount = attempt.Player.PartialCount,
                    FullyCorrectCount = attempt.Player.CorrectCount
                })
                .OrderBy(a => a.Start)
        })
        .OrderBy(r => r.User.Name)
        .ThenBy(r => r.Challenge.Name);

        if (parameters.Sort.IsNotEmpty())
        {
            switch (parameters.Sort.ToLower())
            {
                case "attempts":
                    records = records.Sort(r => r.Attempts.Count(), parameters.SortDirection);
                    break;
                case "best-date":
                    records = records.Sort(r => r.Attempts.OrderByDescending(a => a.Score).FirstOrDefault()?.Start, parameters.SortDirection);
                    break;
                case "best-score":
                    records = records.Sort(r => r.Attempts.OrderByDescending(a => a.Score).FirstOrDefault()?.Score, parameters.SortDirection);
                    break;
                case "best-time":
                    records = records.Sort(r => r.Attempts.OrderByDescending(a => a.Score).FirstOrDefault()?.DurationMs, parameters.SortDirection);
                    break;
                case "most-recent":
                    records = records.Sort(r => r.Attempts.OrderByDescending(a => a.Start).FirstOrDefault()?.Start, parameters.SortDirection);
                    break;
            }
        }

        return new()
        {
            OverallStats = ungroupedResults.OverallStats,
            Records = records
        };
    }

    public async Task<PracticeModeReportResults> GetResultsByPlayerModePerformance(PracticeModeReportParameters parameters, CancellationToken cancellationToken)
    {
        // the "true" argument here includes competitive records, because we're comparing practice vs competitive performance here
        var ungroupedResults = await BuildUngroupedResults(parameters, true, cancellationToken);
        var allSpecRawScores = await GetSpecRawScores(ungroupedResults.Challenges.Select(c => c.SpecId).ToArray());
        var records = ungroupedResults
            .Challenges
            .GroupBy(r => r.Player.UserId)
            .Select(g =>
            {
                return new PracticeModeReportByPlayerModePerformanceRecord
                {
                    // this is really more of a user entity than a player entity, but the report uses "player" to refer to users on purpose
                    Player = new SimpleEntity { Id = g.Key, Name = g.First().Player?.User?.ApprovedName ?? g.First().Player?.ApprovedName },
                    Sponsor = ungroupedResults.Sponsors.FirstOrDefault(s => s.Id == g.FirstOrDefault()?.Player?.User?.Sponsor?.Id),
                    PracticeStats = CalculateByPlayerPerformanceModeSummary(true, g.ToList(), allSpecRawScores),
                    CompetitiveStats = CalculateByPlayerPerformanceModeSummary(false, g.ToList(), allSpecRawScores)
                };
            })
            .OrderBy(r => r.Player.Name);

        if (parameters.Sort.IsNotEmpty())
        {
            switch (parameters.Sort.ToLower())
            {
                case "attempts-competitive":
                    records = records.Sort(r => r.CompetitiveStats?.TotalChallengesPlayed, parameters.SortDirection);
                    break;
                case "attempts-practice":
                    records = records.Sort(r => r.PracticeStats?.TotalChallengesPlayed, parameters.SortDirection);
                    break;
                case "avg-score-percentile-competitive":
                    records = records.Sort(r => r.CompetitiveStats?.AvgScorePercentile, parameters.SortDirection);
                    break;
                case "avg-score-percentile-practice":
                    records = records.Sort(r => r.PracticeStats?.AvgScorePercentile, parameters.SortDirection);
                    break;
                case "avg-score-pct-competitive":
                    records = records.Sort(r => r.CompetitiveStats?.AvgPctAvailablePointsScored, parameters.SortDirection);
                    break;
                case "avg-score-pct-practice":
                    records = records.Sort(r => r.PracticeStats?.AvgPctAvailablePointsScored, parameters.SortDirection);
                    break;
                case "last-played-competitive":
                    records = records.Sort(r => r.CompetitiveStats?.LastAttemptDate, parameters.SortDirection);
                    break;
                case "last-played-practice":
                    records = records.Sort(r => r.PracticeStats?.LastAttemptDate, parameters.SortDirection);
                    break;
                case "player":
                    records = records.Sort(r => r.Player.Name, parameters.SortDirection);
                    break;
            }

            records = records.ThenBy(r => r.Player.Name);
        }

        return new()
        {
            OverallStats = ungroupedResults.OverallStats,
            Records = records
        };
    }

    // this feels really gross, but i'm going to do this as a separate query, because it needs kind of unrelated information. will discuss
    // the possibility of making the prac/comp thing its own report
    public async Task<PracticeModeReportPlayerModeSummary> GetPlayerModePerformanceSummary(string userId, bool isPractice, CancellationToken cancellationToken)
    {
        // have to grab the specIds to ensure that no challenges are coming back with orphaned specIds :(
        var specIds = await _store.WithNoTracking<Data.ChallengeSpec>().Select(s => s.Id).ToArrayAsync(cancellationToken);
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
                .Include(c => c.Game)
                .Include(c => c.Player)
                    .ThenInclude(p => p.User)
                        .ThenInclude(u => u.Sponsor)
            .Where(c => c.Player.UserId == userId)
            .Where(c => c.PlayerMode == (isPractice ? PlayerMode.Practice : PlayerMode.Competition))
            .Where(c => specIds.Contains(c.SpecId))
            .ToListAsync(cancellationToken);

        // these will all be the same
        var user = challenges.First().Player.User;

        // pull the scores for challenge specs this player played in this mode
        var rawScores = (await GetSpecRawScores(challenges.Select(c => c.SpecId).ToArray())).Where(s => s.IsPractice == isPractice);

        return new()
        {
            Player = new()
            {
                Id = user.Id,
                Name = user.ApprovedName,
                Sponsor = user.Sponsor.ToReportViewModel(),
                HasScoringAttempt = challenges.Any(c => c.Score > 0)
            },
            Challenges = challenges.Select(c => new PracticeModeReportPlayerModeSummaryChallenge
            {
                ChallengeSpec = new SimpleEntity { Id = c.SpecId, Name = c.Name },
                Game = new ReportGameViewModel
                {
                    Id = c.GameId,
                    Name = c.Game.Name,
                    IsTeamGame = c.Game.IsTeamGame(),
                    Series = c.Game.Competition,
                    Season = c.Game.Season,
                    Track = c.Game.Track
                },
                MaxPossibleScore = c.Points,
                PctAvailablePointsScored = c.GetPercentMaxPointsScored(),
                Result = c.Result,
                Score = new decimal(c.Score),
                ScorePercentile = CalculatePlayerChallengePercentile(c.Id, c.SpecId, c.Score, c.Game.IsPracticeMode, rawScores)
            })
        };
    }

    public async Task<IEnumerable<PracticeModeReportCsvRecord>> GetCsvExport(PracticeModeReportParameters parameters, CancellationToken cancellationToken)
    {
        var ungroupedResults = await BuildUngroupedResults(parameters, false, cancellationToken);
        var teams = await _reportsService.GetTeamsByPlayerIds(ungroupedResults.Challenges.Select(c => c.PlayerId).Distinct(), cancellationToken);
        var rawScores = await GetSpecRawScores(ungroupedResults.Specs.Values.Select(s => s.Id).ToArray());

        return ungroupedResults.Challenges.Select(c =>
        {
            var sponsor = ungroupedResults
                .Sponsors
                .FirstOrDefault(s => s.Id == c.Player.Sponsor.Id);

            return new PracticeModeReportCsvRecord
            {
                ChallengeId = c.Id,
                ChallengeName = c.Name,
                ChallengeSpecId = c.SpecId,
                GameId = c.GameId,
                GameName = c.Game.Name,
                PlayerId = c.PlayerId,
                PlayerName = c.Player.ApprovedName,
                SponsorId = sponsor?.Id,
                SponsorName = sponsor?.Name,
                TeamId = c.Player.TeamId,
                TeamName = teams.TryGetValue(c.Player.TeamId, out ReportTeamViewModel value) ? value.Name : null,
                UserId = c.Player.UserId,
                UserName = c.Player.User.ApprovedName,
                DurationMs = c.Duration,
                ChallengeResult = c.Result,
                Score = c.Score,
                MaxPossibleScore = c.Points,
                PctMaxPointsScored = c.GetPercentMaxPointsScored(),
                ScorePercentile = (double?)CalculatePlayerChallengePercentile(c.Id, c.SpecId, c.Score, c.Game.IsPracticeMode, rawScores),
                SessionStart = c.StartTime.IsNotEmpty() ? c.StartTime : null,
                SessionEnd = c.EndTime.IsNotEmpty() ? c.EndTime : null
            };
        });
    }

    public async Task<ChallengeSubmissionCsvRecord[]> GetSubmissionsCsv(string challengeSpecId, PracticeModeReportParameters parameters, CancellationToken cancellationToken)
    {
        var challenges = GetBaseQuery(parameters, false);

        // this csv can be generated for all challenges which meet the criteria, or for a specific one via a parameter
        if (challengeSpecId.IsNotEmpty())
        {
            challenges = challenges.Where(c => c.SpecId == challengeSpecId);
        }

        return await _challengeSubmissionsService.GetSubmissionsCsv(challenges, cancellationToken);
    }

    private PracticeModeReportByPlayerModePerformanceRecordModeSummary CalculateByPlayerPerformanceModeSummary(bool isPractice, IEnumerable<Data.Challenge> challenges, IEnumerable<PracticeModeReportByPlayerModePerformanceChallengeScore> percentileTable)
    {
        var modeChallenges = challenges.Where(c => isPractice == (c.PlayerMode == PlayerMode.Practice));
        var modePercentiles = percentileTable.Where(p => p.IsPractice == isPractice);
        PracticeModeReportByPlayerModePerformanceRecordModeSummary modeStats = null;

        if (modeChallenges.Any())
        {
            var scoringChallenges = modeChallenges.Where(c => c.Points > 0);

            modeStats = new PracticeModeReportByPlayerModePerformanceRecordModeSummary
            {
                LastAttemptDate = modeChallenges
                    .OrderByDescending(c => c.Player.SessionBegin)
                    .Select(c => c.Player.SessionBegin)
                    .FirstOrDefault(),
                TotalChallengesPlayed = modeChallenges.Count(),
                ZeroScoreSolves = modeChallenges.Where(c => c.Result == ChallengeResult.None).Count(),
                PartialSolves = modeChallenges.Where(c => c.Result == ChallengeResult.Partial).Count(),
                CompleteSolves = modeChallenges.Where(c => c.Result == ChallengeResult.Success).Count(),
                AvgPctAvailablePointsScored = scoringChallenges.Count() == 0 ? 0 : scoringChallenges.Average(c => c.GetPercentMaxPointsScored()),
                AvgScorePercentile = modeChallenges
                    .Select
                    (
                        c => CalculatePlayerChallengePercentile(c.Id, c.SpecId, c.Score, isPractice, modePercentiles)
                    )
                    .DefaultIfEmpty()
                    .Average()
            };
        }

        return modeStats;
    }

    private decimal CalculatePlayerChallengePercentile(string challengeId, string specId, double score, bool isPractice, IEnumerable<PracticeModeReportByPlayerModePerformanceChallengeScore> percentileTable)
    {
        Func<PracticeModeReportByPlayerModePerformanceChallengeScore, bool> isOtherChallengeRecord = p =>
            p.IsPractice == isPractice &&
            p.ChallengeSpecId == specId &&
            p.ChallengeId != challengeId;

        var denominator = percentileTable
            .Where(p => isOtherChallengeRecord(p))
            .Count();

        if (denominator == 0)
            return 100;

        var numerator = percentileTable
            .Where(p => isOtherChallengeRecord(p) && p.Score < score)
            .Count();

        return decimal.Divide(numerator, denominator) * 100;
    }

    private async Task<IEnumerable<PracticeModeReportByPlayerModePerformanceChallengeScore>> GetSpecRawScores(string[] specIds)
    {
        return await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => specIds.Contains(c.SpecId))
            .GroupBy(c => new { c.Id, c.SpecId, IsPractice = c.PlayerMode == PlayerMode.Practice })
            .Select(g => new PracticeModeReportByPlayerModePerformanceChallengeScore
            {
                ChallengeId = g.Key.Id,
                ChallengeSpecId = g.Key.SpecId,
                IsPractice = g.Key.IsPractice,
                Score = g
                    .Select(c => c.Score)
                    .Max()
            })
            .ToListAsync();
    }
}
