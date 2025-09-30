// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record FeedbackReportQuery(FeedbackReportParameters Parameters, PagingArgs PagingArgs) : IRequest<ReportResults<FeedbackReportSummaryData, FeedbackReportRecord>>, IReportQuery;

internal sealed class FeedbackReportHandler
(
    IFeedbackReportService feedbackReportService,
    INowService nowService,
    IPagingService pagingService,
    IReportsService reportsService,
    IValidatorService validator
) : IRequestHandler<FeedbackReportQuery, ReportResults<FeedbackReportSummaryData, FeedbackReportRecord>>
{
    private readonly IFeedbackReportService _feedbackReportService = feedbackReportService;
    private readonly IPagingService _pagingService = pagingService;
    private readonly INowService _now = nowService;
    private readonly IReportsService _reportsService = reportsService;
    private readonly IValidatorService _validator = validator;

    public async Task<ReportResults<FeedbackReportSummaryData, FeedbackReportRecord>> Handle(FeedbackReportQuery request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(c => c.Require(PermissionKey.Reports_View))
            .Validate(cancellationToken);

        var results = await _feedbackReportService.GetBaseQuery(request.Parameters, cancellationToken);
        var paged = _pagingService.Page(results, request.PagingArgs);

        return new ReportResults<FeedbackReportSummaryData, FeedbackReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Key = ReportKey.Feedback,
                Title = "Feedback Report",
                Description = await _reportsService.GetDescription(ReportKey.Feedback),
                RunAt = _now.Get()
            },
            OverallStats = new FeedbackReportSummaryData
            {
                QuestionCount = results.Any() ? results.First().Responses.Count() : null,
                ResponseCount = results.Count(),
                UnfinalizedCount = results.Where(r => r.WhenFinalized is null).Count(),
                UniqueChallengesCount = results.Select(r => r.ChallengeSpec?.Id).Where(cid => cid is not null).Distinct().Count(),
                UniqueGamesCount = results.Select(r => r.LogicalGame.Id).Distinct().Count()
            },
            Paging = paged.Paging,
            Records = paged.Items,
        };
    }
}
