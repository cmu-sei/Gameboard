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
    Task<IEnumerable<PracticeModeByChallengeReportRecord>> GetResultsByChallenge(PracticeModeReportParameters parameters, CancellationToken cancellationToken);
    Task<IEnumerable<PracticeModeByUserReportRecord>> GetResultsByUser(PracticeModeReportParameters parameters, CancellationToken cancellationToken);
}

internal class PracticeModeReportService : IPracticeModeReportService
{
    private readonly IReportsService _reportsService;
    private readonly IStore _store;

    public PracticeModeReportService(IReportsService reportsService, IStore store)
        => (_reportsService, _store) = (reportsService, store);

    private sealed class PracticeModeReportUngroupedResults
    {
        public required IEnumerable<Data.Challenge> Challenges { get; set; }
        public required IDictionary<string, Data.ChallengeSpec> Specs { get; set; }
        public required IEnumerable<ReportSponsorViewModel> Sponsors { get; set; }
    }

    private async Task<PracticeModeReportUngroupedResults> BuildUngroupedResults(PracticeModeReportParameters parameters, CancellationToken cancellationToken)
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
        DateTimeOffset? startDate = parameters.AttemptDateStart.HasValue ? parameters.AttemptDateStart.Value.ToUniversalTime() : null;
        DateTimeOffset? endDate = parameters.AttemptDateEnd.HasValue ? parameters.AttemptDateEnd.Value.ToUniversalTime() : null;

        var query = _store
            .List<Data.Challenge>()
                .Include(c => c.Game)
                .Include(c => c.Player)
                    .ThenInclude(p => p.User)
            .Where(c => c.Game.PlayerMode == PlayerMode.Practice);

        if (startDate is not null)
        {
            query = query
                .WhereDateHasValue(c => c.StartTime)
                .Where(c => c.StartTime >= startDate);
        }

        if (endDate is not null)
        {
            query = query
                .WhereDateHasValue(c => c.EndTime)
                .Where(c => c.EndTime <= endDate);
        }

        if (parameters.GameIds is not null && parameters.GameIds.Any())
            query = query.Where(c => parameters.GameIds.Contains(c.GameId));

        if (parameters.SponsorIds is not null && parameters.SponsorIds.Any())
        {
            var sponsorLogos = sponsors
                .Where(s => parameters.SponsorIds.Contains(s.Id))
                .Select(s => s.LogoFileName);

            query = query.Where(c => sponsorLogos.Contains(c.Player.Sponsor));
        }

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
            Specs = specs,
            Sponsors = sponsors
        };
    }

    private PracticeModeReportByChallengePerformance BuildChallengePerformance(IEnumerable<Data.Challenge> attempts, ReportSponsorViewModel sponsor = null)
    {
        // precompute totals so we don't have to do it more than once per spec
        Func<Data.Challenge, bool> sponsorConstraint = (attempts => true);
        if (sponsor is not null)
            sponsorConstraint = (attempt) => attempt.Player.Sponsor == sponsor.LogoFileName;

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
            PlayerCount = attempts.Select(a => a.Player.UserId).Distinct().Count(),
            TotalAttempts = totalAttempts,
            CompleteSolves = completeSolves,
            PercentageCompleteSolved = totalAttempts > 0 ? decimal.Divide(completeSolves, totalAttempts) * 100 : null,
            PartialSolves = partialSolves,
            PercentagePartiallySolved = totalAttempts > 0 ? decimal.Divide(partialSolves, totalAttempts) * 100 : null,
            ZeroScoreSolves = zeroScoreSolves,
            PercentageZeroScoreSolved = totalAttempts > 0 ? decimal.Divide(zeroScoreSolves, totalAttempts) * 100 : null
        };
    }

    public async Task<IEnumerable<PracticeModeByUserReportRecord>> GetResultsByUser(PracticeModeReportParameters parameters, CancellationToken cancellationToken)
    {
        var ungroupedResults = await BuildUngroupedResults(parameters, cancellationToken);

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
                            .FirstOrDefault(c => c.Player.Sponsor.NotEmpty())
                            .Player
                            .Sponsor
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
            Attempts = c.ToList().Select(attempt => new PracticeModeReportAttempt
            {
                Player = new SimpleEntity { Id = attempt.PlayerId, Name = attempt.Player.ApprovedName },
                Team = teams.ContainsKey(attempt.Player.TeamId) ? teams[attempt.Player.TeamId] : null,
                Sponsor = ungroupedResults.Sponsors.First(s => s.LogoFileName == attempt.Player.Sponsor),
                Start = attempt.StartTime,
                End = attempt.EndTime,
                DurationMs = attempt.Player.Time,
                Score = attempt.Score,
                PartiallyCorrectCount = attempt.Player.PartialCount,
                FullyCorrectCount = attempt.Player.CorrectCount
            })
        });

        return records;
    }

    public async Task<IEnumerable<PracticeModeByChallengeReportRecord>> GetResultsByChallenge(PracticeModeReportParameters parameters, CancellationToken cancellationToken)
    {
        var ungroupedResults = await BuildUngroupedResults(parameters, cancellationToken);
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

        return records;
    }



    // public async Task<IEnumerable<Data.Challenge>> GetPlayerPracticeCompetitiveResults(PracticeModeReportParameters parameters, CancellationToken cancellationToken)
    // {
    // var userCompetitiveChallenges = await _store
    // .List<Data.Challenge>()
    // .Include(c => c.Player)
    // .Include(c => c.Game)
    // .Where(c => c.Game.PlayerMode == PlayerMode.Competition)
    // .Where(c => userIds.Contains(c.Player.UserId))
    // .GroupBy(c => c.Player.UserId)
    // .ToDictionaryAsync(c => c.Key, c => c, cancellationToken);
    //     CompetitiveSummary = !userCompetitiveChallenges.ContainsKey(c.Key.UserId)
    //             ? null : new PracticeModeUserCompetitiveSummary
    //             {
    //                 AvgCompetitivePointsPct = userCompetitiveChallenges[c.Key.UserId]
    //                     .Where(c => c.Points > 0) // no dividing by zero here
    //                     .Average(c => c.Score / c.Points)
    //                     * 100,
    //                 CompetitiveChallengesPlayed = userCompetitiveChallenges[c.Key.UserId].Count(),
    //                 CompetitiveGamesPlayed = userCompetitiveChallenges[c.Key.UserId]
    //                     .Select(c => c.GameId)
    //                     .Distinct()
    //                     .Count(),
    //                 LastCompetitiveChallengeDate = userCompetitiveChallenges[c.Key.UserId]
    //                     .OrderBy(c => c.StartTime)
    //                     .Select(c => c.StartTime)
    //                     .FirstOrDefault()
    //             }
    // }
}
