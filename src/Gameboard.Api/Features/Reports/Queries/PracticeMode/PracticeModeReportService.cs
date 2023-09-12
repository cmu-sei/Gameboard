using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IPracticeModeReportService
{
    Task<PracticeModeReportResults> GetResultsByChallenge(PracticeModeReportParameters parameters, CancellationToken cancellationToken);
    Task<PracticeModeReportResults> GetResultsByUser(PracticeModeReportParameters parameters, CancellationToken cancellationToken);
    Task<PracticeModeReportResults> GetResultsByPlayerModePerformance(PracticeModeReportParameters parameters, CancellationToken cancellationToken);
    Task<IEnumerable<PracticeModeReportCsvRecord>> GetCsvExport(PracticeModeReportParameters parameters, CancellationToken cancellationToken);
    Task<PracticeModeReportPlayerModeSummary> GetPlayerModePerformanceSummary(string userId, bool isPractice, CancellationToken cancellationToken);
}

internal class PracticeModeReportService : IPracticeModeReportService
{
    private readonly IReportsService _reportsService;
    private readonly IStore _store;

    public PracticeModeReportService(IReportsService reportsService, IStore store)
        => (_reportsService, _store) = (reportsService, store);

    private sealed class PracticeModeReportUngroupedResults
    {
        public required PracticeModeReportOverallStats OverallStats { get; set; }
        public required IEnumerable<Data.Challenge> Challenges { get; set; }
        public required IDictionary<string, Data.ChallengeSpec> Specs { get; set; }
        public required IEnumerable<ReportSponsorViewModel> Sponsors { get; set; }
    }

    private async Task<PracticeModeReportUngroupedResults> BuildUngroupedResults(PracticeModeReportParameters parameters, bool includeCompetitive, CancellationToken cancellationToken)
    {
        // load sponsors - we need them for the report data and they can't be joined
        var sponsors = await _store
            .List<Data.Sponsor>()
            .Select(s => new ReportSponsorViewModel
            {
                Id = s.Id,
                Name = s.Name,
                LogoFileName = s.Logo
            })
            .ToArrayAsync(cancellationToken);

        // process parameters
        DateTimeOffset? startDate = parameters.PracticeDateStart.HasValue ? parameters.PracticeDateStart.Value.ToUniversalTime() : null;
        DateTimeOffset? endDate = parameters.PracticeDateEnd.HasValue ? parameters.PracticeDateEnd.Value.ToUniversalTime() : null;
        var gameIds = _reportsService.ParseMultiSelectCriteria(parameters.Games);
        var sponsorIds = _reportsService.ParseMultiSelectCriteria(parameters.Sponsors);
        var seasons = _reportsService.ParseMultiSelectCriteria(parameters.Seasons);
        var series = _reportsService.ParseMultiSelectCriteria(parameters.Series);
        var tracks = _reportsService.ParseMultiSelectCriteria(parameters.Tracks);

        var query = _store
            .List<Data.Challenge>()
                .Include(c => c.Game)
                .Include(c => c.Player)
                    .ThenInclude(p => p.User)
            .Where(c => includeCompetitive || c.PlayerMode == PlayerMode.Practice);

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

        if (parameters.Seasons.IsNotEmpty())
            query = query.Where(c => parameters.Seasons.Contains(c.Game.Season));

        if (parameters.Series.IsNotEmpty())
            query = query.Where(c => parameters.Series.Contains(c.Game.Competition));

        if (parameters.Tracks.IsNotEmpty())
            query = query.Where(c => parameters.Tracks.Contains(c.Game.Track));

        if (parameters.Games is not null && parameters.Games.Any())
            query = query.Where(c => parameters.Games.Contains(c.GameId));

        if (parameters.Sponsors is not null && parameters.Sponsors.Any())
        {
            var sponsorIds = sponsors
                .Where(s => parameters.Sponsors.Contains(s.Id))
                .Select(s => s.LogoFileName);

            query = query.Where(c => sponsorIds.Contains(c.Player.Sponsor.Id));
        }

        // we have to constrain the query results by eliminating challenges that have a specId
        // which points at a nonexistent spec. (This is possible due to the non-FK relationship
        // between challenge and spec and the fact that specs are deletable)
        // 
        // so load all spec ids and add a clause which excludes challenges with orphaned specIds
        var allSpecIds = await _store.List<Data.ChallengeSpec>().Select(s => s.Id).ToArrayAsync(cancellationToken);
        query = query.Where(c => allSpecIds.Contains(c.SpecId));

        // query for the raw results
        var challenges = await query.ToListAsync(cancellationToken);

        // also load challenge spec data for these challenges (spec can't be joined)
        var specs = await _store
            .List<Data.ChallengeSpec>()
            .Include(s => s.Game)
            .Where(s => challenges.Select(c => c.SpecId).Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s, cancellationToken);

        return new PracticeModeReportUngroupedResults
        {
            Challenges = challenges,
            OverallStats = new()
            {
                AttemptCount = challenges.Count,
                ChallengeCount = challenges
                    .Select(c => c.SpecId)
                    .Distinct()
                    .Count(),
                PlayerCount = challenges
                    .Select(c => c.Player.UserId)
                    .Distinct()
                    .Count(),
                SponsorCount = challenges
                    .Select(c => c.Player.Sponsor)
                    .Distinct()
                    .Count()
            },
            Specs = specs,
            Sponsors = sponsors ?? Array.Empty<ReportSponsorViewModel>()
        };
    }

    private PracticeModeReportByChallengePerformance BuildChallengePerformance(IEnumerable<Data.Challenge> attempts, ReportSponsorViewModel sponsor = null)
    {
        // precompute totals so we don't have to do it more than once per spec
        Func<Data.Challenge, bool> sponsorConstraint = (attempts => true);
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
                Select
                (
                    a => new SimpleEntity { Id = a.Player.UserId, Name = a.Player.ApprovedName }
                )
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

    public async Task<PracticeModeReportResults> GetResultsByChallenge(PracticeModeReportParameters parameters, CancellationToken cancellationToken)
    {
        // the "false" argument here excludes competitive records (this grouping only looks at practice challenges)
        var ungroupedResults = await BuildUngroupedResults(parameters, false, cancellationToken);

        var records = ungroupedResults
            .Challenges
            .GroupBy(c => c.SpecId)
            .Select(g =>
            {
                var attempts = g.ToList();
                var spec = ungroupedResults.Specs[g.Key];

                var sponsorLogosPlayed = attempts.Select(a => a.Player.Sponsor).Distinct();
                var sponsorsPlayed = ungroupedResults
                    .Sponsors
                    .Where(s => sponsorLogosPlayed.Contains(s.LogoFileName));

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
                    Text = spec.Text,
                    SponsorsPlayed = sponsorsPlayed,
                    OverallPerformance = performanceOverall,
                    PerformanceBySponsor = performanceBySponsor
                };
            });

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
                    (s =>
                        s.LogoFileName == c
                            .OrderByDescending(c => c.StartTime)
                            .FirstOrDefault(c => c.Player.Sponsor is not null)?.Player?.Sponsor
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
                MaxPossibleScore = specs[c.Key.SpecId].Points
            },
            Attempts = c
                .ToList()
                .Select(attempt => new PracticeModeReportAttempt
                {
                    Player = new SimpleEntity { Id = attempt.PlayerId, Name = attempt.Player.ApprovedName },
                    Team = teams.ContainsKey(attempt.Player.TeamId) ? teams[attempt.Player.TeamId] : null,
                    Sponsor = ungroupedResults.Sponsors.FirstOrDefault(s => s.LogoFileName == attempt.Player.Sponsor),
                    Start = attempt.StartTime,
                    End = attempt.EndTime,
                    DurationMs = attempt.Player.Time,
                    Result = attempt.Result,
                    Score = attempt.Score,
                    PartiallyCorrectCount = attempt.Player.PartialCount,
                    FullyCorrectCount = attempt.Player.CorrectCount
                })
                .OrderBy(a => a.Start)
        });

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

        return new()
        {
            OverallStats = ungroupedResults.OverallStats,
            Records = ungroupedResults
                .Challenges
                .GroupBy(r => r.Player.UserId)
                .Select(g =>
                {
                    return new PracticeModeReportByPlayerModePerformanceRecord
                    {
                        // this is really more of a user entity than a player entity, but the report uses "player" to refer to users on purpose
                        Player = new SimpleEntity { Id = g.Key, Name = g.First().Player?.User?.ApprovedName ?? g.First().Player?.ApprovedName },
                        Sponsor = ungroupedResults.Sponsors.FirstOrDefault(s => s.LogoFileName == g.FirstOrDefault()?.Player?.User?.Sponsor),
                        PracticeStats = CalculateByPlayerPerformanceModeSummary(true, g.ToList(), allSpecRawScores),
                        CompetitiveStats = CalculateByPlayerPerformanceModeSummary(false, g.ToList(), allSpecRawScores)
                    };
                })
        };
    }

    // this feels really gross, but i'm going to do this as a separate query, because it needs kind of unrelated information. will discuss
    // the possibility of making the prac/comp thing its own report
    public async Task<PracticeModeReportPlayerModeSummary> GetPlayerModePerformanceSummary(string userId, bool isPractice, CancellationToken cancellationToken)
    {
        // have to grab the specIds to ensure that no challenges are coming back with orphaned specIds :(
        var specIds = await _store.List<Data.ChallengeSpec>().Select(s => s.Id).ToArrayAsync(cancellationToken);
        var challenges = await _store
            .List<Data.Challenge>()
                .Include(c => c.Game)
                .Include(c => c.Player)
                    .ThenInclude(p => p.User)
            .Where(c => c.Player.UserId == userId)
            .Where(c => c.PlayerMode == (isPractice ? PlayerMode.Practice : PlayerMode.Competition))
            .Where(c => specIds.Contains(c.SpecId))
            .ToListAsync(cancellationToken);

        // these will all be the same
        var user = challenges.First().Player.User;
        var sponsor = await _store.List<Data.Sponsor>()
            .Select(s => new ReportSponsorViewModel
            {
                Id = s.Id,
                Name = s.Name,
                LogoFileName = s.Logo
            })
            .SingleAsync(s => s.LogoFileName == user.Sponsor, cancellationToken);

        // pull the scores for challenge specs this player played in this mode
        var rawScores = (await GetSpecRawScores(challenges.Select(c => c.SpecId).ToArray())).Where(s => s.IsPractice == isPractice);

        return new()
        {
            Player = new()
            {
                Id = user.Id,
                Name = user.ApprovedName,
                Sponsor = sponsor,
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
        var teams = await _reportsService.GetTeamsByPlayerIds(ungroupedResults.Challenges.Select(c => c.PlayerId), cancellationToken);
        var rawScores = await GetSpecRawScores(ungroupedResults.Specs.Values.Select(s => s.Id).ToArray());

        return ungroupedResults.Challenges.Select(c =>
        {
            var sponsor = ungroupedResults
                .Sponsors
                .FirstOrDefault(s => s.LogoFileName == c.Player.Sponsor);

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
                TeamName = teams.ContainsKey(c.Player.TeamId) ? teams[c.Player.TeamId].Name : null,
                UserId = c.Player.UserId,
                UserName = c.Player.User.ApprovedName,
                DurationMs = c.Duration,
                ChallengeResult = c.Result,
                Score = c.Score,
                MaxPossibleScore = c.Points,
                PctMaxPointsScored = c.GetPercentMaxPointsScored(),
                ScorePercentile = (double?)CalculatePlayerChallengePercentile(c.Id, c.SpecId, c.Score, c.Game.IsPracticeMode, rawScores),
                SessionStart = c.StartTime.HasValue() ? c.StartTime : null,
                SessionEnd = c.EndTime.HasValue() ? c.EndTime : null
            };
        });
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

    private async Task<IEnumerable<PracticeModeReportByPlayerModePerformanceChallengeScore>> GetSpecRawScores(IList<string> specIds)
    {
        return await _store
            .List<Data.Challenge>()
                .Include(c => c.Game)
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
            .Where(k => specIds.Contains(k.ChallengeSpecId))
            .ToListAsync();
    }
}
