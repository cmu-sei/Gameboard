using System;
using System.Linq;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IPlayersReportService
{
    IQueryable<PlayersReportRecord> GetQuery(PlayersReportParameters parameters);
}

internal class PlayersReportService(IReportsService reportsService, IStore store) : IPlayersReportService
{
    private readonly IReportsService _reportsService = reportsService;
    private readonly IStore _store = store;

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
                .Where(u => u.CreatedOn <= parameters.CreatedDateEnd.Value.ToEndDate().ToUniversalTime());

        if (gamesCriteria.Any())
            query = query
                .Where(u => u.Enrollments.Any(p => gamesCriteria.Contains(p.GameId)));

        if (parameters.LastPlayedDateStart is not null)
            query = query
                .Where(u => u.Enrollments.FirstOrDefault() != null && u.Enrollments.First().SessionBegin >= parameters.LastPlayedDateStart.Value.ToUniversalTime());

        if (parameters.LastPlayedDateEnd is not null)
            query = query
                .Where(u => u.Enrollments.FirstOrDefault() != null && u.Enrollments.First().SessionBegin <= parameters.LastPlayedDateEnd.Value.ToEndDate().ToUniversalTime());

        if (seasonsCriteria.Any())
            query = query.Where(u => u.Enrollments.Any(p => seasonsCriteria.Contains(p.Game.Season.ToLower())));

        if (seriesCriteria.Any())
            query = query.Where(u => u.Enrollments.Any(p => seriesCriteria.Contains(p.Game.Competition.ToLower())));

        if (sponsorCriteria.Any())
            query = query
                .Where(u => sponsorCriteria.Contains(u.SponsorId));

        if (tracksCriteria.Any())
            query = query.Where(u => u.Enrollments.Any(p => tracksCriteria.Contains(p.Game.Track.ToLower())));

        var composedQuery = query.Select(u => new PlayersReportRecord
        {
            User = new SimpleEntity { Id = u.Id, Name = u.Name },
            Sponsor = new ReportSponsorViewModel
            {
                Id = u.SponsorId,
                Name = u.Sponsor.Name,
                LogoFileName = u.Sponsor.Logo,
            },
            CreatedOn = u.CreatedOn,
            LastPlayedOn = u.Enrollments.Where(p => p.SessionBegin > DateTimeOffset.MinValue).Any() ?
                u.Enrollments
                    .Where(p => p.SessionBegin > DateTimeOffset.MinValue)
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
            DistinctGamesPlayed = u
                .Enrollments
                .Select(p => p.Game)
                .Select(g => g.Name)
                .Where(gId => gId != null && gId != string.Empty)
                .Distinct(),
            DistinctSeasonsPlayed = u
                .Enrollments
                .Select(p => p.Game)
                .Select(g => g.Season)
                .Where(s => s != null && s != string.Empty)
                .Distinct(),
            DistinctSeriesPlayed = u
                .Enrollments
                .Select(p => p.Game)
                .Select(g => g.Competition)
                .Where(s => s != null & s != string.Empty)
                .Distinct(),
            DistinctTracksPlayed = u
                .Enrollments
                .Select(p => p.Game)
                .Select(g => g.Track)
                .Where(t => t != null & t != string.Empty)
                .Distinct(),
        });

        var orderedQuery = composedQuery.OrderBy(r => r.User.Name);

        if (parameters.Sort.IsNotEmpty())
        {
            switch (parameters.Sort)
            {
                case "account-created":
                    orderedQuery = orderedQuery.Sort(r => r.CreatedOn, parameters.SortDirection);
                    break;
                case "count-competitive":
                    orderedQuery = orderedQuery.Sort(r => r.DeployedCompetitiveChallengesCount, parameters.SortDirection);
                    break;
                case "count-practice":
                    orderedQuery = orderedQuery.Sort(r => r.DeployedPracticeChallengesCount, parameters.SortDirection);
                    break;
                case "count-games":
                    orderedQuery = orderedQuery.Sort(r => r.DistinctGamesPlayed.Count(), parameters.SortDirection);
                    break;
                case "count-seasons":
                    orderedQuery = orderedQuery.Sort(r => r.DistinctSeasonsPlayed.Count(), parameters.SortDirection);
                    break;
                case "count-series":
                    orderedQuery = orderedQuery.Sort(r => r.DistinctSeriesPlayed.Count(), parameters.SortDirection);
                    break;
                case "count-tracks":
                    orderedQuery = orderedQuery.Sort(r => r.DistinctSeriesPlayed.Count(), parameters.SortDirection);
                    break;
                case "last-played":
                    orderedQuery = orderedQuery.Sort(r => r.LastPlayedOn, parameters.SortDirection);
                    break;
                case "name":
                    orderedQuery = orderedQuery.Sort(r => r.User.Name, parameters.SortDirection);
                    break;
            }

            orderedQuery = orderedQuery.ThenBy(r => r.User.Name);
        }

        return orderedQuery;
    }
}
