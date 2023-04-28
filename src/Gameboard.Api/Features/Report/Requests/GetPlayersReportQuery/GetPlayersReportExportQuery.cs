using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record GetPlayersReportExportQuery(PlayersReportQueryParameters Parameters) : IRequest<IEnumerable<PlayersReportCsvRecord>>;

public class GetPlayersReportExportHandler : IRequestHandler<GetPlayersReportExportQuery, IEnumerable<PlayersReportCsvRecord>>
{
    private readonly IReportsService _reportsService;

    public GetPlayersReportExportHandler(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    public async Task<IEnumerable<PlayersReportCsvRecord>> Handle(GetPlayersReportExportQuery request, CancellationToken cancellationToken)
    {
        var query = _reportsService.GetPlayersReportBaseQuery(request.Parameters);

        return await query.Select(p => new PlayersReportCsvRecord
        {
            Id = p.User.Id,
            Name = p.User.ApprovedName,
            SponsorName = p.Sponsor,
            Competition = p.Game.Competition,
            Track = p.Game.Track,
            GameId = p.GameId,
            GameName = p.Game.Name,
            Challenges = p.Challenges.Select(c => new PlayersReportCsvRecordChallenge
            {
                ChallengeId = c.Id,
                ChallengeName = c.Name,
                ChallengeScore = c.Score
            }),
            PlayerId = p.Id,
            PlayerName = p.ApprovedName,
            MaxPossibleScore = p.Game.Specs.Sum(s => s.Points),
            Score = p.Score
        }).ToArrayAsync();
    }
}
