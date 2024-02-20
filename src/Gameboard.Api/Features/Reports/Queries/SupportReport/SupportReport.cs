using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record SupportReportQuery(SupportReportParameters Parameters, PagingArgs PagingArgs, User ActingUser) : IRequest<ReportResults<SupportReportStatSummary, SupportReportRecord>>, IReportQuery;

internal class SupportReportQueryHandler : IRequestHandler<SupportReportQuery, ReportResults<SupportReportStatSummary, SupportReportRecord>>
{
    private readonly IReportsService _reportsService;
    private readonly ISupportReportService _service;
    private readonly ReportsQueryValidator _validator;

    public SupportReportQueryHandler
    (
        IReportsService reportsService,
        ISupportReportService service,
        ReportsQueryValidator validator
    )
    {
        _reportsService = reportsService;
        _service = service;
        _validator = validator;
    }

    public async Task<ReportResults<SupportReportStatSummary, SupportReportRecord>> Handle(SupportReportQuery request, CancellationToken cancellationToken)
    {
        // validate access
        await _validator.Validate(request, cancellationToken);

        // build the results and summary
        var records = await _service.QueryRecords(request.Parameters);
        var stats = _service.GetStatSummary(records);

        return _reportsService.BuildResults(new ReportRawResults<SupportReportStatSummary, SupportReportRecord>
        {
            PagingArgs = request.PagingArgs,
            ParameterSummary = null,
            Records = await _service.QueryRecords(request.Parameters),
            ReportKey = ReportKey.Support,
            Title = "Support Report",
            OverallStats = stats
        });
    }
}
