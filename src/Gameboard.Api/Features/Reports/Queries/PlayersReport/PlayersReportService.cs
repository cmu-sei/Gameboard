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
        var gamesCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Games);
        var seasonsCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Seasons);
        var seriesCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Series);
        var sponsorCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Sponsors);
        var tracksCriteria = _reportsService.ParseMultiSelectCriteria(parameters.Tracks);

        var query = _store
            .WithNoTracking<Data.User>()
            .Include(u => u.Enrollments.OrderByDescending(p => p.SessionBegin))
                .ThenInclude(p => p.Game)
            .Include(u => u.Enrollments.OrderByDescending(p => p.SessionBegin))
                .ThenInclude(p => p.Challenges)
            .Include(u => u.Sponsor)
            .OrderBy(u => u.ApprovedName)
                .ThenBy(u => u.Sponsor.Name)
            // split because of the massive joins
            .AsSplitQuery()
            // make the final type of the query base an
            // IQueryable<Data.User>
            .Where(u => true);

        if (parameters.CreatedDateStart is not null)
            query = query
                .WhereDateIsNotEmpty(u => u.CreatedOn)
                .Where(u => u.CreatedOn >= parameters.CreatedDateStart.Value.ToUniversalTime());

        if (parameters.CreatedDateEnd is not null)
            query = query
                .WhereDateIsNotEmpty(u => u.CreatedOn)
                .Where(u => u.CreatedOn <= parameters.CreatedDateEnd.Value.ToUniversalTime());

        if (gamesCriteria.Any())
            query = query
                .Where(u => u.Enrollments.Any(g => gamesCriteria.Contains(g.Id)));

        if (parameters.LastPlayedDateStart is not null)
            query = query
                .Where(u => u.Enrollments.FirstOrDefault() != null && u.Enrollments.First().SessionBegin >= parameters.LastPlayedDateStart.Value.ToUniversalTime());

        if (parameters.LastPlayedDateEnd is not null)
            query = query
                .Where(u => u.Enrollments.FirstOrDefault() != null && u.Enrollments.First().SessionBegin <= parameters.LastPlayedDateEnd.Value.ToEndDate().ToUniversalTime());

        if (seasonsCriteria.Any())
            query = query.Where(u => u.Enrollments.Any(p => seasonsCriteria.Contains(p.Game.Season)));

        if (seriesCriteria.Any())
            query = query.Where(u => u.Enrollments.Any(p => seriesCriteria.Contains(p.Game.Competition)));

        if (sponsorCriteria.Any())
            query = query
                .Where(u => sponsorCriteria.Contains(u.SponsorId));

        if (tracksCriteria.Any())
            query = query.Where(u => u.Enrollments.Any(p => tracksCriteria.Contains(p.Game.Track)));

        return query.Select(u => new PlayersReportRecord
        {
            User = new SimpleEntity { Id = u.Id, Name = u.Name },
            Sponsor = new ReportSponsorViewModel
            {
                Id = u.SponsorId,
                Name = u.Sponsor.Name,
                LogoFileName = u.Sponsor.Logo,
            },
            CreatedOn = u.CreatedOn,
            LastPlayedOn = u
                .Enrollments
                .OrderByDescending(p => p.SessionBegin)
                .FirstOrDefault() != null ?
                u
                    .Enrollments
                    .OrderByDescending(p => p.SessionBegin)
                    .First().SessionBegin :
                    null,
            CompletedCompetitiveChallengesCount = u
                .Enrollments
                .SelectMany(p => p.Challenges)
                .Where(c => c.PlayerMode == PlayerMode.Competition)
                .Where(c => c.Score >= c.Points)
                .Count(),
            CompletedPracticeChallengesCount = u
                .Enrollments
                .SelectMany(p => p.Challenges)
                .Where(c => c.PlayerMode == PlayerMode.Practice)
                .Where(c => c.Score >= c.Points)
                .Count(),
            DeployedCompetitiveChallengesCount = u
                .Enrollments
                .SelectMany(p => p.Challenges)
                .Where(c => c.PlayerMode == PlayerMode.Competition)
                .Count(),
            DeployedPracticeChallengesCount = u
                .Enrollments
                .SelectMany(p => p.Challenges)
                .Where(c => c.PlayerMode == PlayerMode.Practice)
                .Count(),
            DistinctGamesPlayedCount = u
                .Enrollments
                .Select(p => p.GameId)
                .Where(gId => gId != null && gId != string.Empty)
                .Distinct()
                .Count(),
            DistinctSeasonsPlayedCount = u
                .Enrollments
                .Select(p => p.Game.Season)
                .Where(s => s != null && s != string.Empty)
                .Distinct()
                .Count(),
            DistinctSeriesPlayedCount = u
                .Enrollments
                .Select(p => p.Game.Competition)
                .Where(s => s != null & s != string.Empty)
                .Distinct()
                .Count(),
            DistinctTracksPlayedCount = u
                .Enrollments
                .Select(p => p.Game.Track)
                .Where(t => t != null & t != string.Empty)
                .Distinct()
                .Count()
        });
    }
}
