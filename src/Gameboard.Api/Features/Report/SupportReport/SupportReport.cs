using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record SupportReportQuery(SupportReportParameters Parameters) : IRequest<ReportResults<SupportReportRecord>>;

internal class SupportReportQueryHandler : IRequestHandler<SupportReportQuery, ReportResults<SupportReportRecord>>
{
    private readonly INowService _now;
    private readonly ISupportReportService _service;

    public SupportReportQueryHandler
    (
        INowService now,
        ISupportReportService service
    )
    {
        _now = now;
        _service = service;
    }

    public async Task<ReportResults<SupportReportRecord>> Handle(SupportReportQuery request, CancellationToken cancellationToken)
    {
        return new ReportResults<SupportReportRecord>
        {
            MetaData = new ReportMetaData
            {
                Title = "Support Report",
                RunAt = _now.Get(),
                Key = ReportKey.SupportReport
            },
            Paging = null,
            Records = await _service.QueryRecords(request.Parameters)
        };
    }
}
