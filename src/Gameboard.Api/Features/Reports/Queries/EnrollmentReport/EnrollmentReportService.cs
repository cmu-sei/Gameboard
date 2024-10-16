using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IEnrollmentReportService
{
    IQueryable<Data.Player> GetBaseQuery(EnrollmentReportParameters parameters);
    Task<IOrderedEnumerable<EnrollmentReportRecord>> GetRawResults(EnrollmentReportParameters parameters, CancellationToken cancellationToken);
    Task<EnrollmentReportStatSummary> GetSummaryStats(EnrollmentReportParameters parameters, CancellationToken cancellationToken);
}

internal class EnrollmentReportService(
    IReportsService reportsService,
    IStore store
    ) : IEnrollmentReportService
{
    private readonly IReportsService _reportsService = reportsService;
    private readonly IStore _store = store;

    public IQueryable<Data.Player> GetBaseQuery(EnrollmentReportParameters parameters)
    {
        // parse multiselect criteria
        var gamesCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Games);
        var seasonCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Seasons);
        var seriesCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Series);
        var sponsorCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Sponsors);
        var trackCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Tracks);
        DateTimeOffset? enrollDateStart = parameters.EnrollDateStart.HasValue ? parameters.EnrollDateStart.Value.ToEndDate().ToUniversalTime() : null;
        DateTimeOffset? enrollDateEnd = parameters.EnrollDateEnd.HasValue ? parameters.EnrollDateEnd.Value.ToEndDate().ToUniversalTime() : null;

        // the fundamental unit of reporting here is really the player record (an "enrollment"), so resolve enrollments that
        // meet the filter criteria (and have at least one challenge completed in competitive mode)
        var query = _store
            .WithNoTracking<Data.Player>()
            .AsSplitQuery()
            .Include(p => p.Game)
            .Include(p => p.Challenges.Where(c => c.PlayerMode == PlayerMode.Competition))
            .Include(p => p.User)
            .Include(p => p.Sponsor)
            // to be included in this report, the player record must have either no challenges OR have
            // all of their challenges be in competitive mode
            .Where(p => p.Challenges.Count() == 0 || p.Challenges.All(c => c.PlayerMode == PlayerMode.Competition))
            .Where(p => p.Mode == PlayerMode.Competition)
            .Where(p => p.TeamId != null && p.TeamId != string.Empty);

        if (enrollDateStart != null)
            query = query
                .WhereDateIsNotEmpty(p => p.WhenCreated)
                .Where(p => p.WhenCreated >= enrollDateStart);

        if (enrollDateEnd != null)
            query = query
                .WhereDateIsNotEmpty(p => p.WhenCreated)
                .Where(p => p.WhenCreated <= enrollDateEnd);

        if (gamesCriteria.Any())
            query = query.Where(p => gamesCriteria.Contains(p.GameId));

        if (seasonCriteria.Any())
            query = query.Where(p => seasonCriteria.Contains(p.Game.Season.ToLower()));

        if (seriesCriteria.Any())
            query = query.Where(p => seriesCriteria.Contains(p.Game.Competition.ToLower()));

        if (trackCriteria.Any())
            query = query.Where(p => trackCriteria.Contains(p.Game.Track.ToLower()));

        if (sponsorCriteria.Any())
            query = query.Where(p => sponsorCriteria.Contains(p.Sponsor.Id));

        return query;
    }

    public async Task<IOrderedEnumerable<EnrollmentReportRecord>> GetRawResults(EnrollmentReportParameters parameters, CancellationToken cancellationToken)
    {
        // finalize query - we have to do the rest "client" (application server) side
        var players = await GetBaseQuery(parameters).ToArrayAsync(cancellationToken);

        // This is pretty messy. Here's why:
        //
        // Teams are not first-class entities in the data model as of now. There's a teamId
        // on the player record which is always populated (even if the game is not a team game)
        // and there is another on the Challenge entity (which is also always populated). These
        // are not foreign keys and can't be the bases of join-like structures in EF.
        //
        // Additionally, the semantics of who owns a challenge vary between individual and team games.
        // As of now, when a team starts a challenge, a nearly-random (.First()) player is chosen and
        // assigned as the owner of the challenge. For the purposes of this report, this means that if
        // we strictly look at individual player registrations and report their challenges and performance,
        // we won't get the whole story if their challenges are owned by a teammate.
        //
        // To accommodate this, we just group all players by team id, create a dictionary of challenges
        // owned by any player on the team (by TeamId), and report the team's challenges for every player
        // on the team.
        var teamChallengeData = players
            .Select(p => new
            {
                p.Id,
                p.TeamId,
                Name = p.ApprovedName,
                p.Role,
                p.Score,
                p.Sponsor,
                Challenges = p.Challenges.Select(c => new EnrollmentReportChallengeQueryData
                {
                    SpecId = c.SpecId,
                    Name = c.Name,
                    WhenCreated = c.WhenCreated,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    ManualChallengeBonuses = c.AwardedManualBonuses.Select(b => new EnrollmentReportManualChallengeBonus
                    {
                        Description = b.Description,
                        Points = b.PointValue
                    }),
                    Score = c.Score,
                    MaxPossiblePoints = c.Points
                })
            })
            .GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // transform the player records into enrollment report records
        var records = players.Select(p =>
        {
            var playerTeamChallengeData = teamChallengeData[p.TeamId];
            var captain = playerTeamChallengeData.FirstOrDefault(p => p.Role == PlayerRole.Manager);
            var playerTeamSponsorLogos = playerTeamChallengeData.Select(p => p.Sponsor);
            var challenges = teamChallengeData[p.TeamId]
                .SelectMany(c => ChallengeDataToViewModel(c.Challenges))
                .DistinctBy(c => c.SpecId)
                .ToArray();

            return new EnrollmentReportRecord
            {
                User = new SimpleEntity { Id = p.UserId, Name = p.User.ApprovedName },
                Player = new EnrollmentReportPlayerViewModel
                {
                    Id = p.Id,
                    Name = p.ApprovedName,
                    EnrollDate = p.WhenCreated.HasValue() ? p.WhenCreated : null,
                    Sponsor = p.Sponsor.ToReportViewModel()
                },
                Game = new ReportGameViewModel
                {
                    Id = p.GameId,
                    Name = p.Game.Name,
                    IsTeamGame = p.Game.MinTeamSize > 1,
                    Series = p.Game.Competition,
                    Season = p.Game.Season,
                    Track = p.Game.Track
                },
                Team = new EnrollmentReportTeamViewModel
                {
                    Id = p.TeamId,
                    Name = captain?.Name ?? p.Name,
                    CurrentCaptain = new SimpleEntity { Id = captain?.Id ?? p.Id, Name = captain?.Name ?? p.Name },
                    Sponsors = playerTeamChallengeData.Select(p => p.Sponsor.ToReportViewModel())
                },
                PlayTime = new EnrollmentReportPlayTimeViewModel
                {
                    Start = p.SessionBegin.HasValue() ? p.SessionBegin : null,
                    DurationMs = p.Time > 0 ? p.Time : null,
                    End = (p.SessionBegin.HasValue() && p.Time > 0) ? p.SessionBegin.AddMilliseconds(p.Time) : null
                },
                Challenges = challenges,
                ChallengeCount = challenges.Length,
                ChallengesPartiallySolvedCount = challenges.Where(c => c.Result == ChallengeResult.Partial).Count(),
                ChallengesCompletelySolvedCount = challenges.Where(c => c.Result == ChallengeResult.Success).Count(),
                Score = p.Score
            };
        });

        return records
            .OrderBy(r => r.Player.Name)
            .ThenBy(r => r.Game.Name);
    }

    public async Task<EnrollmentReportStatSummary> GetSummaryStats(EnrollmentReportParameters parameters, CancellationToken cancellationToken)
    {
        var query = GetBaseQuery(parameters)
            .Select(p => new
            {
                Player = p,
                ChallengesStarted = p.Challenges.Where(c => c.StartTime != DateTimeOffset.MinValue).Count()
            });

        var rawResults = await query.ToArrayAsync(cancellationToken);

        var usersBySponsor = rawResults
            .Where(p => p.Player.Sponsor is not null)
            .Select(p => new
            {
                p.Player.SponsorId,
                p.Player.UserId
            })
            .DistinctBy(sponsorUser => new { sponsorUser.SponsorId, sponsorUser.UserId })
            .GroupBy(r => r.SponsorId)
            .OrderByDescending(g => g.DistinctBy(su => su.UserId).Count())
            .ToDictionary(g => g.Key, g => g.DistinctBy(su => su.UserId).Count());

        // resolve the sponsor with the most unique users
        // (it'll be the first one if there are any, because they're ordered by player
        // count above)
        var sponsorWithMostPlayers = default(EnrollmentReportStatSummarySponsorPlayerCount);
        if (usersBySponsor.Count != 0)
        {
            var allSponsors = rawResults.Select(p => p.Player.Sponsor).DistinctBy(s => s.Id);
            var sponsor = allSponsors.FirstOrDefault(s => s.Id == usersBySponsor.First().Key);

            if (sponsor is not null)
            {
                sponsorWithMostPlayers = new()
                {
                    Sponsor = new ReportSponsorViewModel
                    {
                        Id = sponsor.Id,
                        Name = sponsor.Name,
                        LogoFileName = sponsor.Logo
                    },
                    DistinctPlayerCount = usersBySponsor[sponsor.Id]
                };
            }
        }

        return new EnrollmentReportStatSummary
        {
            DistinctGameCount = rawResults.Select(r => r.Player.GameId).Distinct().Count(),
            DistinctPlayerCount = rawResults.Select(r => r.Player.UserId).Distinct().Count(),
            DistinctSponsorCount = rawResults.Select(r => r.Player.SponsorId).Distinct().Count(),
            DistinctTeamCount = rawResults.Select(r => r.Player.TeamId).Distinct().Count(),
            SponsorWithMostPlayers = sponsorWithMostPlayers,
            TeamsWithNoSessionCount = rawResults.Where(r => r.Player.SessionBegin.IsEmpty()).Select(r => r.Player.TeamId).Distinct().Count(),
            TeamsWithNoStartedChallengeCount = rawResults.Where(r => r.ChallengesStarted == 0).Select(r => r.Player.TeamId).Distinct().Count()
        };
    }

    private IEnumerable<EnrollmentReportChallengeViewModel> ChallengeDataToViewModel(IEnumerable<EnrollmentReportChallengeQueryData> challengeData)
        => challengeData.Select(c => new EnrollmentReportChallengeViewModel
        {
            Name = c.Name,
            SpecId = c.SpecId,
            DeployDate = c.WhenCreated,
            StartDate = c.StartTime,
            EndDate = c.EndTime,
            DurationMs = c.StartTime.HasValue() && c.EndTime.HasValue() ? c.EndTime.Subtract(c.StartTime).TotalMilliseconds : null,
            Result = ChallengeExtensions.GetResult(c.Score, c.MaxPossiblePoints),
            ManualChallengeBonuses = c.ManualChallengeBonuses,
            Score = c.Score,
            MaxPossiblePoints = c.MaxPossiblePoints
        });
}
