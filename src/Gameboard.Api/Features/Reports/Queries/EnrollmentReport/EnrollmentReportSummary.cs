using System.Linq;
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
    private readonly IReportsService _reportsService;

    public EnrollmentReportSummaryHandler
    (
        IEnrollmentReportService enrollmentReportService,
        INowService now,
        IPagingService pagingService,
        ReportsQueryValidator reportsQueryValidator,
        IReportsService reportsService
    )
    {
        _enrollmentReportService = enrollmentReportService;
        _now = now;
        _pagingService = pagingService;
        _reportsQueryValidator = reportsQueryValidator;
        _reportsService = reportsService;
    }

    public async Task<ReportResults<EnrollmentReportRecord>> Handle(EnrollmentReportSummaryQuery request, CancellationToken cancellationToken)
    {
        // validate
        await _reportsQueryValidator.Validate(request, cancellationToken);

        // pull, sort, and page results
        var records = await _enrollmentReportService.GetRawResults(request.Parameters, cancellationToken);

        if (request.Parameters.Sort.IsNotEmpty())
        {
            var sortDirection = request.Parameters.SortDirection;

            switch (request.Parameters.Sort)
            {
                case "count-attempted":
                    records = records.Sort(r => r.ChallengeCount, sortDirection);
                    break;
                case "count-complete":
                    records = records.Sort(r => r.ChallengesCompletelySolvedCount, sortDirection);
                    break;
                case "count-solve-partial":
                    records = records.Sort(r => r.ChallengesPartiallySolvedCount, sortDirection);
                    break;
                case "player":
                    records = records.Sort(r => r.Player.Name, sortDirection);
                    break;
                case "enroll-date":
                    records = records.Sort(r => r.Player.EnrollDate, sortDirection);
                    break;
                case "game":
                    records = records.Sort(r => r.Game.Name, sortDirection);
                    break;
                case "time":
                    records = records.Sort(r => r.PlayTime, sortDirection);
                    break;
            }

            records = records
                .ThenBy(r => r.Player.Name)
                .ThenBy(r => r.Game.Name);
        }

        var paged = _pagingService.Page(records, request.PagingArgs);

        return new ReportResults<EnrollmentReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Description = await _reportsService.GetDescription(ReportKey.Enrollment),
                Title = "Enrollment Report",
                RunAt = _now.Get(),
                Key = ReportKey.Enrollment
            },
            Records = paged.Items,
            Paging = paged.Paging
        };
    }
}
