using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record GetMetaDataQuery(string ReportKey, User ActingUser) : IRequest<ReportMetaData>, IReportQuery;

internal class GetMetaDataHandler : IRequestHandler<GetMetaDataQuery, ReportMetaData>
{
    private readonly INowService _now;
    private readonly IReportsService _reportsService;
    private readonly ReportsQueryValidator _reportsQueryValidator;

    public GetMetaDataHandler
    (
        INowService now,
        IReportsService reportsService,
        ReportsQueryValidator reportsQueryValidator
    )
        => (_now, _reportsService, _reportsQueryValidator) = (now, reportsService, reportsQueryValidator);

    public async Task<ReportMetaData> Handle(GetMetaDataQuery request, CancellationToken cancellationToken)
    {
        await _reportsQueryValidator.Validate(request);

        var reports = await _reportsService.List();
        var report = reports.FirstOrDefault(r => r.Key == request.ReportKey) ?? throw new ResourceNotFound<ReportViewModel>(request.ReportKey);

        return new ReportMetaData
        {
            Key = request.ReportKey,
            Title = report.Name,
            ParametersSummary = null,
            RunAt = _now.Get()
        };
    }
}
