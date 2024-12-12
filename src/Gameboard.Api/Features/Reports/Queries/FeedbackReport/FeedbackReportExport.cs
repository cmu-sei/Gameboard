using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record FeedbackReportExportQuery(FeedbackReportParameters Parameters) : IRequest<IEnumerable<FeedbackReportExportRecord>>;

internal sealed class FeedbackReportExportHandler
(
    IFeedbackReportService reportService,
    IValidatorService validatorService
) : IRequestHandler<FeedbackReportExportQuery, IEnumerable<FeedbackReportExportRecord>>
{
    private readonly IFeedbackReportService _reportService = reportService;
    private readonly IValidatorService _validator = validatorService;

    public async Task<IEnumerable<FeedbackReportExportRecord>> Handle(FeedbackReportExportQuery request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.RequirePermissions(PermissionKey.Reports_View))
            .Validate(cancellationToken);

        var results = await _reportService.GetBaseQuery(request.Parameters, cancellationToken);

        return results.Select(r => new FeedbackReportExportRecord
        {
            Id = r.Id,
            ChallengeSpecId = r.ChallengeSpec?.Id,
            ChallengeSpecName = r.ChallengeSpec?.Name,
            GameId = r.LogicalGame.Id,
            GameName = r.LogicalGame.Name,
            GameSeason = r.LogicalGame.Season,
            GameSeries = r.LogicalGame.Series,
            GameTrack = r.LogicalGame.Track,
            IsTeamGame = r.LogicalGame.IsTeamGame,
            SponsorId = r.Sponsor.Id,
            SponsorName = r.Sponsor.Name,
            UserId = r.User.Id,
            UserName = r.User.Name,
            WhenCreated = r.WhenCreated,
            WhenEdited = r.WhenEdited,
            WhenFinalized = r.WhenFinalized
        }).ToArray();
    }
}
