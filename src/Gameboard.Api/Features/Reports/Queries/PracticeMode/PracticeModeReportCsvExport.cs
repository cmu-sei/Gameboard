// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record PracticeModeReportCsvExportQuery(PracticeModeReportParameters Parameters) : IReportQuery, IRequest<IEnumerable<PracticeModeReportCsvRecord>>;

internal class PracticeModeReportCsvExportHandler(IPracticeModeReportService practiceModeReportService, ReportsQueryValidator validator) : IRequestHandler<PracticeModeReportCsvExportQuery, IEnumerable<PracticeModeReportCsvRecord>>
{
    private readonly IPracticeModeReportService _practiceModeReportService = practiceModeReportService;
    private readonly ReportsQueryValidator _validator = validator;

    public async Task<IEnumerable<PracticeModeReportCsvRecord>> Handle(PracticeModeReportCsvExportQuery request, CancellationToken cancellationToken)
    {
        await _validator.Validate(request, cancellationToken);
        return await _practiceModeReportService.GetCsvExport(request.Parameters, cancellationToken);
    }
}
