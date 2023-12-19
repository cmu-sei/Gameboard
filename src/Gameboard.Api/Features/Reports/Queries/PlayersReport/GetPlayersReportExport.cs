using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record GetPlayersReportExportQuery(PlayersReportParameters Parameters, User ActingUser) : IRequest<IEnumerable<PlayersReportCsvRecord>>, IReportQuery;

internal class GetPlayersReportExportHandler : IRequestHandler<GetPlayersReportExportQuery, IEnumerable<PlayersReportCsvRecord>>
{
    private readonly IPlayersReportService _playersReportService;
    private readonly ReportsQueryValidator _reportsQueryValidator;

    public GetPlayersReportExportHandler
    (
        IPlayersReportService playersReportService,
        ReportsQueryValidator reportsQueryValidator
    )
    {
        _playersReportService = playersReportService;
        _reportsQueryValidator = reportsQueryValidator;
    }

    public async Task<IEnumerable<PlayersReportCsvRecord>> Handle(GetPlayersReportExportQuery request, CancellationToken cancellationToken)
    {
        // validate / authorize
        await _reportsQueryValidator.Validate(request, cancellationToken);

        return await _playersReportService
            .GetQuery(request.Parameters)
            .Select(r => new PlayersReportCsvRecord
            {
                UserId = r.User.Id,
                UserName = r.User.Name,
                SponsorId = r.Sponsor.Id,
                SponsorName = r.Sponsor.Name,
                CreatedOn = r.CreatedOn,
                LastPlayedOn = r.LastPlayedOn,
                CompletedCompetitiveChallengesCount = r.CompletedCompetitiveChallengesCount,
                CompletedPracticeChallengesCount = r.CompletedPracticeChallengesCount,
                DeployedCompetitiveChallengesCount = r.DeployedCompetitiveChallengesCount,
                DeployedPracticeChallengesCount = r.DeployedPracticeChallengesCount,
                DistinctGamesPlayedCount = r.DistinctGamesPlayed.Count(),
                DistinctSeasonsPlayedCount = r.DistinctSeasonsPlayed.Count(),
                DistinctSeriesPlayedCount = r.DistinctSeriesPlayed.Count(),
                DistinctTracksPlayedCount = r.DistinctTracksPlayed.Count()
            })
            .ToArrayAsync(cancellationToken);
    }
}
