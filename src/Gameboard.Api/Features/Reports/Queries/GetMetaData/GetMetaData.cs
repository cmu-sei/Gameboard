using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record GetMetaDataQuery(string ReportKey) : IRequest<ReportMetaData>, IReportQuery;

internal class GetMetaDataHandler
(
    INowService nowService,
    IReportsService reportsService,
    ReportsQueryValidator reportsQueryValidator,
    ISlugService slugger
) : IRequestHandler<GetMetaDataQuery, ReportMetaData>
{
    public async Task<ReportMetaData> Handle(GetMetaDataQuery request, CancellationToken cancellationToken)
    {
        await reportsQueryValidator.Validate(request, cancellationToken);

        var reports = await reportsService.List();
        var normalizedKey = slugger.Get(request.ReportKey);
        var report = reports.FirstOrDefault(r => string.Equals(slugger.Get(r.Key), normalizedKey, StringComparison.InvariantCultureIgnoreCase)) ?? throw new ResourceNotFound<ReportViewModel>(request.ReportKey);

        return new ReportMetaData
        {
            Key = request.ReportKey,
            Title = report.Name,
            Description = report.Description,
            IsExportable = report.IsExportable,
            ParametersSummary = null,
            RunAt = nowService.Get()
        };
    }
}
