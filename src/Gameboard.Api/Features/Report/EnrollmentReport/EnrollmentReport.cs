using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using Gameboard.Api.Services;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record EnrollmentReportQuery(EnrollmentReportParameters Parameters, PagingArgs PagingArgs) : IRequest<ReportResults<EnrollmentReportRecord>>;

internal class EnrollmentReportQueryHandler : IRequestHandler<EnrollmentReportQuery, ReportResults<EnrollmentReportRecord>>
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

    public async Task<ReportResults<EnrollmentReportRecord>> Handle(EnrollmentReportQuery request, CancellationToken cancellationToken)
    {
        var unpagedResults = await _enrollmentReportService.GetRecords(request.Parameters, cancellationToken);
        var paged = _pagingService.Page(unpagedResults, request.PagingArgs);

        return new ReportResults<EnrollmentReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Title = "Enrollment Report",
                RunAt = _now.Get(),
                Key = ReportKey.EnrollmentReport
            },
            Records = paged.Items,
            Paging = paged.Paging
        };
    }
}
