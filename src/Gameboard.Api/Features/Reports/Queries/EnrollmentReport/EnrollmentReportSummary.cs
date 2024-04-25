using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record EnrollmentReportSummaryQuery(EnrollmentReportParameters Parameters, PagingArgs PagingArgs) : IRequest<ReportResults<EnrollmentReportRecord>>, IReportQuery;

internal class EnrollmentReportSummaryHandler : IRequestHandler<EnrollmentReportSummaryQuery, ReportResults<EnrollmentReportRecord>>
{
    private readonly IEnrollmentReportService _enrollmentReportService;
    private readonly INowService _now;
    private readonly IPagingService _pagingService;
    private readonly ReportsQueryValidator _reportsQueryValidator;

    public EnrollmentReportSummaryHandler
    (
        IEnrollmentReportService enrollmentReportService,
        INowService now,
        IPagingService pagingService,
        ReportsQueryValidator reportsQueryValidator
    )
    {
        _enrollmentReportService = enrollmentReportService;
        _now = now;
        _pagingService = pagingService;
        _reportsQueryValidator = reportsQueryValidator;
    }

    public async Task<ReportResults<EnrollmentReportRecord>> Handle(EnrollmentReportSummaryQuery request, CancellationToken cancellationToken)
    {
        // validate
        await _reportsQueryValidator.Validate(request, cancellationToken);

        // pull and page results
        var records = await _enrollmentReportService.GetRawResults(request.Parameters, cancellationToken);
        var paged = _pagingService.Page(records, request.PagingArgs);

        return new ReportResults<EnrollmentReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Title = "Enrollment Report",
                RunAt = _now.Get(),
                Key = ReportKey.Enrollment
            },
            Records = paged.Items,
            Paging = paged.Paging
        };
    }
}
