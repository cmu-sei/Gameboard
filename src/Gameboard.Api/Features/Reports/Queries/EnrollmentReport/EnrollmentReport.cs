using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using Gameboard.Api.Services;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record EnrollmentReportQuery(EnrollmentReportParameters Parameters, PagingArgs PagingArgs) : IRequest<ReportResults<EnrollmentReportStatSummary, EnrollmentReportRecord>>;

internal class EnrollmentReportQueryHandler : IRequestHandler<EnrollmentReportQuery, ReportResults<EnrollmentReportStatSummary, EnrollmentReportRecord>>
{
    private readonly IEnrollmentReportService _enrollmentReportService;
    private readonly INowService _now;
    private readonly IPagingService _pagingService;

    public EnrollmentReportQueryHandler
    (
        IEnrollmentReportService enrollmentReportService,
        INowService now,
        IPagingService pagingService
    )
    {
        _enrollmentReportService = enrollmentReportService;
        _now = now;
        _pagingService = pagingService;
    }

    public async Task<ReportResults<EnrollmentReportStatSummary, EnrollmentReportRecord>> Handle(EnrollmentReportQuery request, CancellationToken cancellationToken)
    {
        var rawResults = await _enrollmentReportService.GetRawResults(request.Parameters, cancellationToken);
        var paged = _pagingService.Page(rawResults.Records, request.PagingArgs);

        return new ReportResults<EnrollmentReportStatSummary, EnrollmentReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Title = "Enrollment Report",
                RunAt = _now.Get(),
                Key = ReportKey.Enrollment
            },
            OverallStats = rawResults.StatSummary,
            Records = paged.Items,
            Paging = paged.Paging
        };
    }
}
