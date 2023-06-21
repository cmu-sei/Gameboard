using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record EnrollmentReportQuery(EnrollmentReportParameters Parameters) : IRequest<ReportResults<EnrollmentReportRecord>>;

internal class EnrollmentReportQueryHandler : IRequestHandler<EnrollmentReportQuery, ReportResults<EnrollmentReportRecord>>
{
    private readonly IEnrollmentReportService _enrollmentReportService;
    private readonly INowService _now;

    public EnrollmentReportQueryHandler
    (
        IEnrollmentReportService enrollmentReportService,
        INowService now
    )
    {
        _enrollmentReportService = enrollmentReportService;
        _now = now;
    }

    public async Task<ReportResults<EnrollmentReportRecord>> Handle(EnrollmentReportQuery request, CancellationToken cancellationToken)
    {
        var records = await _enrollmentReportService.GetRecords(request.Parameters, cancellationToken);

        return new ReportResults<EnrollmentReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Title = "Enrollment Report",
                Key = ReportKey.EnrollmentReport,
                RunAt = _now.Get()
            },
            Records = records
        };
    }
}
