using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record EnrollmentReportSummaryStatsQuery(EnrollmentReportParameters Parameters, User ActingUser) : IRequest<EnrollmentReportStatSummary>, IReportQuery;

internal class EnrollmentReportSummaryStatsHandler : IRequestHandler<EnrollmentReportSummaryStatsQuery, EnrollmentReportStatSummary>
{
    private readonly IEnrollmentReportService _enrollmentReportService;
    private readonly ReportsQueryValidator _reportsQueryValidator;

    public EnrollmentReportSummaryStatsHandler
    (
        IEnrollmentReportService enrollmentReportService,
        ReportsQueryValidator reportsQueryValidator
    )
    {
        _enrollmentReportService = enrollmentReportService;
        _reportsQueryValidator = reportsQueryValidator;
    }

    public async Task<EnrollmentReportStatSummary> Handle(EnrollmentReportSummaryStatsQuery request, CancellationToken cancellationToken)
    {
        // validate
        await _reportsQueryValidator.Validate(request, cancellationToken);

        return await _enrollmentReportService.GetSummaryStats(request.Parameters, cancellationToken);
    }
}
