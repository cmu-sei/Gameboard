using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record PracticeModeReportQuery(PracticeModeReportParameters Parameters) : IRequest<ReportResults<PracticeModeReportRecord>>;

internal class PracticeModeReportHandler : IRequestHandler<PracticeModeReportQuery, ReportResults<PracticeModeReportRecord>>
{
    public Task<ReportResults<PracticeModeReportRecord>> Handle(PracticeModeReportQuery request, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
