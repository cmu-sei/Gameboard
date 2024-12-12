using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record FeedbackReportExportQuery(FeedbackReportParameters Parameters) : IRequest<FeedbackReportExportContainer>;

internal sealed class FeedbackReportExportHandler
(
    IFeedbackReportService reportService,
    IValidatorService validatorService
) : IRequestHandler<FeedbackReportExportQuery, FeedbackReportExportContainer>
{
    private readonly IFeedbackReportService _reportService = reportService;
    private readonly IValidatorService _validator = validatorService;

    public async Task<FeedbackReportExportContainer> Handle(FeedbackReportExportQuery request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.RequirePermissions(PermissionKey.Reports_View))
            .Validate(cancellationToken);

        var results = await _reportService.GetBaseQuery(request.Parameters, cancellationToken);
        // we have to use the dynamic type here to accommodate the questions/answers
        var records = new List<dynamic>();
        foreach (var r in results)
        {
            dynamic record = new ExpandoObject();

            record.Id = r.Id;
            record.ChallengeSpecId = r.ChallengeSpec?.Id;
            record.ChallengeSpecName = r.ChallengeSpec?.Name;
            record.GameId = r.LogicalGame.Id;
            record.GameName = r.LogicalGame.Name;
            record.GameSeason = r.LogicalGame.Season;
            record.GameSeries = r.LogicalGame.Series;
            record.GameTrack = r.LogicalGame.Track;
            record.IsTeamGame = r.LogicalGame.IsTeamGame;
            record.SponsorId = r.Sponsor.Id;
            record.SponsorName = r.Sponsor.Name;
            record.UserId = r.User.Id;
            record.UserName = r.User.Name;
            record.WhenCreated = r.WhenCreated;
            record.WhenEdited = r.WhenEdited;
            record.WhenFinalized = r.WhenFinalized;

            var asDict = record as IDictionary<string, object>;
            foreach (var question in r.Responses)
            {
                asDict.Add(question.Prompt, question.Answer);
            }

            records.Add(asDict);
        }

        return new FeedbackReportExportContainer { Records = records };
    }
}
