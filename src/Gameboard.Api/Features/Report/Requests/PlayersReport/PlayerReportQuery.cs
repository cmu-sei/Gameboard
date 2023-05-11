using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record PlayersReportQuery(PlayersReportQueryParameters Parameters) : IRequest<ReportResults<PlayersReportRecord>>;

internal class GetPlayersReportQueryHandler : IRequestHandler<PlayersReportQuery, ReportResults<PlayersReportRecord>>
{
    private readonly INowService _nowService;
    private readonly IPlayerStore _playerStore;
    private readonly IReportStore _reportStore;
    private readonly IReportsService _reportsService;
    private readonly ISponsorStore _sponsorStore;

    public GetPlayersReportQueryHandler
    (
        INowService now,
        IPlayerStore playerStore,
        IReportStore reportStore,
        IReportsService reportsService,
        ISponsorStore sponsorStore
    )
    {
        _nowService = now;
        _playerStore = playerStore;
        _reportStore = reportStore;
        _reportsService = reportsService;
        _sponsorStore = sponsorStore;
    }

    public async Task<ReportResults<PlayersReportRecord>> Handle(PlayersReportQuery request, CancellationToken cancellationToken)
    {
        var query = _reportsService.GetPlayersReportBaseQuery(request.Parameters);
        return await TransformQueryToResults(query);
    }

    internal async Task<ReportResults<PlayersReportRecord>> TransformQueryToResults(IQueryable<Data.Player> query)
    {
        var users = await query
            .GroupBy(p => p.UserId)
            .ToDictionaryAsync(p => p.Key, p => p.ToList());

        var sponsors = await _sponsorStore.ListWithNoTracking().ToArrayAsync();

        var records = users.Select(u =>
        {
            var playerRecords = u.Value;
            var games = playerRecords.Select(p => p.Game);
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

        return new ReportResults<PlayersReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Key = ReportKey.PlayersReport,
                Title = "Players Report",
                RunAt = _nowService.Get(),
            },
            Records = records
        };
    }
}
