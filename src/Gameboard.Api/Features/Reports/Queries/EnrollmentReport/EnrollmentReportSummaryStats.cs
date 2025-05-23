using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record EnrollmentReportSummaryStatsQuery(EnrollmentReportParameters Parameters) : IRequest<EnrollmentReportStatSummary>, IReportQuery;

internal class EnrollmentReportSummaryStatsHandler
(
    IEnrollmentReportService enrollmentReportService,
    ReportsQueryValidator reportsQueryValidator
) : IRequestHandler<EnrollmentReportSummaryStatsQuery, EnrollmentReportStatSummary>
{
    public async Task<EnrollmentReportStatSummary> Handle(EnrollmentReportSummaryStatsQuery request, CancellationToken cancellationToken)
    {
        // validate
        await reportsQueryValidator.Validate(request, cancellationToken);

        return await enrollmentReportService.GetSummaryStats(request.Parameters, cancellationToken);
    }
}
