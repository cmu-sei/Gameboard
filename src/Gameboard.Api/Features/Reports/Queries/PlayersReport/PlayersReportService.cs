using System;
using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Reports;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public interface IPlayersReportService
{
    IQueryable<PlayersReportRecord> GetQuery(PlayersReportParameters parameters);
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
                    u.Enrollments.FirstOrDefault() != null ?
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

        if (parameters.SponsorId.IsNotEmpty())
            query = query
                .Where(u => u.SponsorId == parameters.SponsorId);

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
        });
    }
}
