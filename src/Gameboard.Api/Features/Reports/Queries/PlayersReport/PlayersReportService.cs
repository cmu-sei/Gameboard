using System;
using System.Linq;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Reports;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IPlayersReportService
{
    // IQueryable<PlayersReportRecord> GetQuery(PlayersReportParameters parameters);
}

internal class PlayersReportService : IPlayersReportService
{
    private readonly IStore _store;

    public PlayersReportService
    (
        IStore store
    )
    {
        _store = store;
    }

    public IQueryable<PlayersReportRecord> GetQuery(PlayersReportParameters parameters)
    {
        // var query = _store
        //     .WithNoTracking<Data.User>()
        //     .Include(u => u.Enrollments.OrderByDescending(p => p.SessionBegin))
        //         .ThenInclude(p => p.Game)
        //     .Include(u => )
        //     .Include(u => u.Sponsor)
        //     .GroupBy(u => u.Id)
        //     .Select(u => new
        //     {
        //         UserId = u.Key,
        //         UserName = u.First().ApprovedName,
        //         SponsorId = u.First().SponsorId,
        //         SponsorName = u.First().Sponsor.Name,
        //         SponsorLogoFile = u.First().Sponsor.Logo,
        //         CreatedOn = u.First().CreatedOn,
        //         LastPlayedOn = u.First().Enrollments.FirstOrDefault(),
        //         DeployedCompetitiveChallengeCount = u.First().Enrollments.

        //     })
        //     // make the final type of the query base an
        //     // IQueryable<Data.User>
        //     .Where(u => true);

        // if (parameters.CreatedDateStart is not null)
        //     query = query
        //         .WhereDateIsNotEmpty(u => u.CreatedOn)
        //         .Where(u => u.CreatedOn >= parameters.CreatedDateStart.Value);

        // if (parameters.CreatedDateEnd is not null)
        //     query = query
        //         .WhereDateIsNotEmpty(u => u.CreatedOn)
        //         .Where(u => u.CreatedOn <= parameters.CreatedDateEnd.Value);

        // if (parameters.SponsorId.IsNotEmpty())
        //     query = query
        //         .Where(u => u.SponsorId == parameters.SponsorId);

        // return query.Select(u => new PlayersReportRecord
        // {
        //     User = new SimpleEntity { Id = u.Id, Name = u.ApprovedName },
        //     Sponsor = new ReportSponsorViewModel
        //     {
        //         Id = u.SponsorId,
        //         Name = u.Sponsor.Name,
        //         LogoFileName = u.Sponsor.Logo
        //     },
        //     CreatedOn = u.CreatedOn,

        // });
        throw new NotImplementedException();
    }
}
