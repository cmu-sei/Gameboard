using System;
using System.Linq;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IPlayersReportService
{
    IQueryable<PlayersReportRecord> GetQuery(PlayersReportParameters parameters);
}

internal class PlayersReportService : IPlayersReportService
{
    private readonly IReportsService _reportsService;
    private readonly IStore _store;

    public PlayersReportService(IReportsService reportsService, IStore store)
    {
        _reportsService = reportsService;
        _store = store;
    }

    public IQueryable<PlayersReportRecord> GetQuery(PlayersReportParameters parameters)
    {
        var sponsorCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Sponsors);

        var query = _store
            .WithNoTracking<Data.User>()
            .Include(u => u.Enrollments.OrderByDescending(p => p.SessionBegin))
                .ThenInclude(p => p.Game)
            .Include(u => u.Enrollments.OrderByDescending(p => p.SessionBegin))
                .ThenInclude(p => p.Challenges)
            .Include(u => u.Sponsor)
            .Select(u => new
            {
                UserId = u.Id,
                UserName = u.ApprovedName,
                SponsorId = u.SponsorId,
                SponsorName = u.Sponsor.Name,
                SponsorLogoFile = u.Sponsor.Logo,
                CreatedOn = u.CreatedOn,
                LastPlayedOn =
                (
                    u.Enrollments.OrderByDescending(p => p.SessionBegin).FirstOrDefault() != null ?
                        u.Enrollments.First().SessionBegin :
                        null as DateTimeOffset?
                ),
                DeployedCompetitiveChallengeCount = u
                    .Enrollments
                    .SelectMany(p => p.Challenges)
                    .Where(c => c.PlayerMode == PlayerMode.Competition)
                    .Count(),
                DeployedPracticeChallengeCount = u
                    .Enrollments
                    .SelectMany(p => p.Challenges)
                    .Where(c => c.PlayerMode == PlayerMode.Practice)
                    .Count(),
                DistinctSeriesPlayed = u
                    .Enrollments
                    .Select(p => p.Game)
                    .Select(g => g.Competition)
                    .Where(c => c != string.Empty && c != null)
                    .Distinct()
                    .Count(),
                DistinctTracksPlayed = u
                    .Enrollments
                    .Select(p => p.Game)
                    .Select(g => g.Track)
                    .Where(t => t != string.Empty && t != null)
                    .Distinct()
                    .Count(),
                DistinctSeasonsPlayed = u
                    .Enrollments
                    .Select(p => p.Game)
                    .Select(g => g.Season)
                    .Where(s => s != string.Empty && s != null)
                    .Distinct()
                    .Count(),
                DistinctGamesPlayed = u
                    .Enrollments
                    .Select(p => p.GameId)
                    .Distinct()
                    .Count()
            })
            // make the final type of the query base an
            // IQueryable<Data.User>
            .Where(u => true);

        if (parameters.CreatedDateStart is not null)
            query = query
                .WhereDateIsNotEmpty(u => u.CreatedOn)
                .Where(u => u.CreatedOn >= parameters.CreatedDateStart.Value);

        if (parameters.CreatedDateEnd is not null)
            query = query
                .WhereDateIsNotEmpty(u => u.CreatedOn)
                .Where(u => u.CreatedOn <= parameters.CreatedDateEnd.Value);

        if (parameters.LastPlayedDateStart is not null)
            query = query
                .Where(u => u.LastPlayedOn != null && u.LastPlayedOn >= parameters.LastPlayedDateStart.Value);

        if (parameters.LastPlayedDateEnd is not null)
            query = query
                .Where(u => u.LastPlayedOn <= parameters.LastPlayedDateEnd.Value.ToEndDate());

        if (sponsorCriteria.Any())
            query = query
                .Where(u => sponsorCriteria.Contains(u.SponsorId));

        return query.Select(u => new PlayersReportRecord
        {
            User = new SimpleEntity { Id = u.UserId, Name = u.UserName },
            Sponsor = new ReportSponsorViewModel
            {
                Id = u.SponsorId,
                Name = u.SponsorName,
                LogoFileName = u.SponsorLogoFile,
            },
            CreatedOn = u.CreatedOn,
            LastPlayedOn = u.LastPlayedOn,
            DeployedCompetitiveChallengesCount = u.DeployedCompetitiveChallengeCount,
            DeployedPracticeChallengesCount = u.DeployedPracticeChallengeCount,
            DistinctGamesPlayedCount = u.DistinctGamesPlayed,
            DistinctSeasonsPlayedCount = u.DistinctSeasonsPlayed,
            DistinctSeriesPlayedCount = u.DistinctSeriesPlayed,
            DistinctTracksPlayedCount = u.DistinctTracksPlayed
        });
    }
}
