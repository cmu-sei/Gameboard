using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Reports;

public record PlayersReportExportQuery(PlayersReportQueryParameters Parameters) : IRequest<IEnumerable<PlayersReportExportRecord>>;

public class PlayersReportExportHandler : IRequestHandler<PlayersReportExportQuery, IEnumerable<PlayersReportExportRecord>>
{
    private readonly IPlayersReportService _reportsService;

    public PlayersReportExportHandler(IPlayersReportService reportsService)
    {
        _reportsService = reportsService;
    }

    public async Task<IEnumerable<PlayersReportExportRecord>> Handle(PlayersReportExportQuery request, CancellationToken cancellationToken)
    {
        var query = _reportsService.GetPlayersReportBaseQuery(request.Parameters);

        return await query.Select(p => new PlayersReportExportRecord
        {
            Id = p.User.Id,
            Name = p.User.ApprovedName,
            SponsorName = p.Sponsor,
            Competition = p.Game.Competition,
            Track = p.Game.Track,
            GameId = p.GameId,
            GameName = p.Game.Name,
            ChallengeSummary = string.Join(", ", p.Challenges.Select(c => $"{c.Name} ({c.Score}/{c.Points})")),
            PlayerId = p.Id,
            PlayerName = p.ApprovedName,
            MaxPossibleScore = p.Game.Specs.Sum(s => s.Points),
            Score = p.Score
        }).ToArrayAsync();
    }
}