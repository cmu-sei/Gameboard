using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record GetPlayersReportQuery(PlayersReportQueryParameters Parameters) : IRequest<PlayersReportResults>;

internal class GetPlayersReportQueryHandler : IRequestHandler<GetPlayersReportQuery, PlayersReportResults>, IReportRequestHandler<PlayersReportQueryParameters, Data.Player, PlayersReportResults>
{
    private readonly INowService _nowService;
    private readonly IPlayerStore _playerStore;
    private readonly IReportStore _reportStore;
    private readonly ISponsorStore _sponsorStore;

    public GetPlayersReportQueryHandler
    (
        INowService now,
        IPlayerStore playerStore,
        IReportStore reportStore,
        ISponsorStore sponsorStore
    )
    {
        _nowService = now;
        _playerStore = playerStore;
        _reportStore = reportStore;
        _sponsorStore = sponsorStore;
    }

    public async Task<PlayersReportResults> Handle(GetPlayersReportQuery request, CancellationToken cancellationToken)
    {
        var query = BuildQuery(request.Parameters);
        return await TransformQueryToResults(query);
    }

    public IQueryable<Data.Player> BuildQuery(PlayersReportQueryParameters parameters)
    {
        var baseQuery = _playerStore
            .ListWithNoTracking()
            .Include(p => p.Game)
            .Include(p => p.Challenges)
            .Include(p => p.User)
            .Where(p => p.Game.PlayerMode == PlayerMode.Competition)
            .AsQueryable();

        if (parameters.SessionStartWindow?.DateStart != null)
        {
            baseQuery = baseQuery.Where(p => p.SessionBegin >= parameters.SessionStartWindow.DateStart);
        }

        if (parameters.SessionStartWindow?.DateEnd != null)
        {
            baseQuery = baseQuery.Where(p => p.SessionBegin >= parameters.SessionStartWindow.DateEnd);
        }

        if (parameters.Competition.NotEmpty())
        {
            baseQuery = baseQuery
                .Where(p => p.Game.Competition == parameters.Competition);
        }

        if (parameters.Track.NotEmpty())
        {
            baseQuery = baseQuery
                .Where(p => p.Game.Track == parameters.Track);
        }

        if (parameters.ChallengeId.NotEmpty())
        {
            baseQuery = baseQuery
                .Where(c => c.Id == parameters.ChallengeId);
        }

        if (parameters.GameId.NotEmpty())
            baseQuery = baseQuery
                .Where(p => p.GameId == parameters.GameId);

        return baseQuery;
    }

    public async Task<PlayersReportResults> TransformQueryToResults(IQueryable<Data.Player> query)
    {
        var users = await query
            .GroupBy(p => p.UserId)
            .ToDictionaryAsync(p => p.Key, p => p.ToList());

        var sponsors = await _sponsorStore.ListWithNoTracking().ToArrayAsync();

        var records = users.Select(u =>
        {
            var playerRecords = u.Value;
            var games = playerRecords.Select(p => p.Game);
            // var challenges = games.ToDictionary(g => g.Id, g => g.Challenges.ToList());
            var challenges = playerRecords.SelectMany(c => c.Challenges);

            return new PlayersReportRecord
            {
                User = new SimpleEntity { Id = u.Key, Name = playerRecords.First().User.Name },
                Sponsors = playerRecords
                    .Select(p => sponsors.First(s => s.Logo == p.Sponsor))
                    .Select(s => new PlayersReportSponsor
                    {
                        Id = s.Id,
                        Name = s.Name,
                        LogoUri = s.Logo
                    })
                    .ToArray(),

                Games = new PlayersReportGamesAndChallengesSummary
                {
                    CountEnrolled = games.Count(),
                    CountDeployed = playerRecords
                        .Where(p => p.Challenges.Any(c => c.StartTime.HasValue())).DistinctBy(c => c.GameId)
                        .DistinctBy(p => p.GameId)
                        .Count(),
                    CountScoredPartial = playerRecords
                        .Where(g => g.Challenges.Any(c => c.Score > 0 && c.Score < c.Points))
                        .DistinctBy(p => p.GameId)
                        .Count(),
                    CountScoredComplete = playerRecords
                        .Where(p => p.Challenges.Any(c => c.Score >= c.Points))
                        .DistinctBy(c => c.GameId)
                        .Count()
                },
                Challenges = new PlayersReportGamesAndChallengesSummary
                {
                    CountEnrolled = challenges.Count(),
                    CountDeployed = challenges.Where(c => c.StartTime.HasValue()).Count(),
                    CountScoredPartial = challenges.Where(c => c.Score > 0 && c.Score < c.Points).Count(),
                    CountScoredComplete = challenges.Where(c => c.Score >= c.Points).Count()
                },
                CompetitionsPlayed = games.Select(g => g.Competition).Distinct(),
                TracksPlayed = games.Select(g => g.Track).Distinct()
            };
        });

        return new PlayersReportResults
        {
            MetaData = new ReportMetaData
            {
                Id = (await _reportStore.List().SingleAsync(r => r.Key == ReportKey.PlayersReport)).Id,
                Title = "Players Report",
                RunAt = _nowService.Get(),
            },
            Records = records
        };
    }
}
