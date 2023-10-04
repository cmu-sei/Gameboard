using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record SupportReportQuery(SupportReportParameters Parameters, PagingArgs PagingArgs, User ActingUser) : IRequest<ReportResults<SupportReportRecord>>, IReportQuery;

internal class SupportReportQueryHandler : IRequestHandler<SupportReportQuery, ReportResults<SupportReportRecord>>
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

    public async Task<ReportResults<SupportReportRecord>> Handle(SupportReportQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);

        return _reportsService.BuildResults(new ReportRawResults<SupportReportRecord>
        {
            PagingArgs = request.PagingArgs,
            ParameterSummary = null,
            Records = await _service.QueryRecords(request.Parameters),
            ReportKey = ReportKey.Support,
            Title = "Support Report"
        });
    }
}
