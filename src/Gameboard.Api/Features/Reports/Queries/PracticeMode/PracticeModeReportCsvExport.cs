using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record PracticeModeReportCsvExportQuery(PracticeModeReportParameters Parameters, User ActingUser) : IReportQuery, IRequest<IEnumerable<PracticeModeReportCsvRecord>>;

internal class PracticeModeReportCsvExportHandler : IRequestHandler<PracticeModeReportCsvExportQuery, IEnumerable<PracticeModeReportCsvRecord>>
{
    private readonly IPracticeModeReportService _practiceModeReportService;
    private readonly ReportsQueryValidator _validator;

    public PracticeModeReportCsvExportHandler(IPracticeModeReportService practiceModeReportService, ReportsQueryValidator validator)
    {
        _practiceModeReportService = practiceModeReportService;
        _validator = validator;
    }

    public async Task<IEnumerable<PracticeModeReportCsvRecord>> Handle(PracticeModeReportCsvExportQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);
        return await _practiceModeReportService.GetCsvExport(request.Parameters, cancellationToken);
    }
}
