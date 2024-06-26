using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Challenges;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record GetChallengesReportQuery(ChallengesReportParameters Parameters, PagingArgs PagingArgs) : IRequest<ReportResults<ChallengesReportStatSummary, ChallengesReportRecord>>, IReportQuery;

internal class GetChallengesReportHandler : IRequestHandler<GetChallengesReportQuery, ReportResults<ChallengesReportStatSummary, ChallengesReportRecord>>
{
    private readonly IChallengesReportService _challengesReportService;
    private readonly IPagingService _pagingService;
    private readonly INowService _nowService;
    private readonly IReportsService _reportsService;
    private readonly ReportsQueryValidator _validator;

    public GetChallengesReportHandler
    (
        IChallengesReportService challengesReportService,
        IPagingService pagingService,
        INowService nowService,
        IReportsService reportsService,
        ReportsQueryValidator validator
    )
    {
        _challengesReportService = challengesReportService;
        _pagingService = pagingService;
        _nowService = nowService;
        _reportsService = reportsService;
        _validator = validator;
    }

    public async Task<ReportResults<ChallengesReportStatSummary, ChallengesReportRecord>> Handle(GetChallengesReportQuery request, CancellationToken cancellationToken)
    {
        // validate/authorize
        await _validator.Validate(request, cancellationToken);

        var rawResults = await _challengesReportService.GetRawResults(request.Parameters, cancellationToken);
        var reportDesc = await _reportsService.GetDescription(ReportKey.Challenges);
        var statSummary = _challengesReportService.GetStatSummary(rawResults);
        var paged = _pagingService.Page(rawResults, request.PagingArgs);

        return new ReportResults<ChallengesReportStatSummary, ChallengesReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Description = reportDesc,
                Key = ReportKey.Challenges,
                Title = "Challenges Report",
                ParametersSummary = null,
                RunAt = _nowService.Get(),
            },
            Paging = paged.Paging,
            Records = paged.Items,
            OverallStats = statSummary
        };
    }
}
