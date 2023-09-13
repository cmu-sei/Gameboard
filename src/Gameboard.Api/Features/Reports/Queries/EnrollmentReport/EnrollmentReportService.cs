using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Common;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IEnrollmentReportService
{
    IQueryable<Data.Player> GetBaseQuery(EnrollmentReportParameters parameters);
    Task<EnrollmentReportRawResults> GetRawResults(EnrollmentReportParameters parameters, CancellationToken cancellationToken);
}

internal class EnrollmentReportService : IEnrollmentReportService
{
    private readonly IReportsService _reportsService;
    private readonly IStore _store;

    public EnrollmentReportService
    (
        IReportsService reportsService,
        IStore store
    )
    {
        _reportsService = reportsService;
        _store = store;
    }

    public IQueryable<Data.Player> GetBaseQuery(EnrollmentReportParameters parameters)
    {
        // parse multiselect criteria
        var gamesCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Games);
        var seasonCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Seasons);
        var seriesCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Series);
        var sponsorCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Sponsors);
        var trackCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Tracks);
        DateTimeOffset? enrollDateStart = parameters.EnrollDateStart.HasValue ? parameters.EnrollDateStart.Value.ToUniversalTime() : null;
        DateTimeOffset? enrollDateEnd = parameters.EnrollDateEnd.HasValue ? parameters.EnrollDateEnd.Value.ToUniversalTime() : null;

        // the fundamental unit of reporting here is really the player record (an "enrollment"), so resolve enrollments that
        // meet the filter criteria (and have at least one challenge completed in competitive mode)
        var query = _store
            .List<Data.Player>()
            .Include(p => p.Game)
            .Include(p => p.Challenges.Where(c => c.PlayerMode == PlayerMode.Competition))
            .Include(p => p.User)
            .Include(p => p.Sponsor)
            .Where(p => p.Challenges.Any(c => c.PlayerMode == PlayerMode.Competition));

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

    public async Task<EnrollmentReportRawResults> GetRawResults(EnrollmentReportParameters parameters, CancellationToken cancellationToken)
    {
        // finalize query - we have to do the rest "client" (application server) side
        var players = await GetBaseQuery(parameters).ToListAsync(cancellationToken);

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
                User = new SimpleEntity { Id = p.UserId, Name = p.User.Name },
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
                    Sponsors = playerTeamChallengeData.Select(p =>
                    {

                    })
                },
                PlayTime = new EnrollmentReportPlayTimeViewModel
                {
                    Start = p.SessionBegin.HasValue() ? p.SessionBegin : null,
                    DurationMs = p.Time > 0 ? p.Time : null,
                    End = (p.SessionBegin.HasValue() && p.Time > 0) ? p.SessionBegin.AddMilliseconds(p.Time) : null
                },
                Challenges = challenges,
                ChallengesPartiallySolvedCount = challenges.Where(c => c.Result == ChallengeResult.Partial).Count(),
                ChallengesCompletelySolvedCount = challenges.Where(c => c.Result == ChallengeResult.Success).Count(),
                Score = p.Score
            };
        });

        var usersBySponsor = records
            .Where(r => r.Player.Sponsor is not null)
            .Select(r => new
            {
                SponsorId = r.Player.Sponsor.Id,
                UserId = r.User.Id
            })
            .DistinctBy(sponsorUser => new { sponsorUser.SponsorId, sponsorUser.UserId })
            .GroupBy(r => r.SponsorId)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Distinct().Count());

        EnrollmentReportStatSummarySponsorPlayerCount sponsorWithMostPlayers = null;

        if (usersBySponsor.Any())
        {
            var sponsor = sponsors.FirstOrDefault(s => s.Id == usersBySponsor.First().Key);

            if (sponsor is not null)
            {
                sponsorWithMostPlayers = new()
                {
                    Sponsor = sponsor,
                    DistinctPlayerCount = usersBySponsor[sponsor.Id]
                };
            }
        }

        var statSummary = new EnrollmentReportStatSummary
        {
            DistinctGameCount = records.Select(r => r.Game.Id).Distinct().Count(),
            DistinctPlayerCount = records.Select(r => r.User.Id).Distinct().Count(),
            DistinctSponsorCount = usersBySponsor.Keys.Count,
            SponsorWithMostPlayers = sponsorWithMostPlayers,
            DistinctTeamCount = records.Select(p => p.Team.Id).Distinct().Count()
        };

        return new()
        {
            StatSummary = statSummary,
            Records = records
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
