using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record GetMetaDataQuery(string ReportKey) : IRequest<ReportMetaData>;

internal class GetMetaDataHandler : IRequestHandler<GetMetaDataQuery, ReportMetaData>
{
    private readonly INowService _now;
    private readonly IReportsService _reportsService;
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public GetMetaDataHandler
    (
        INowService now,
        IReportsService reportsService,
        UserRoleAuthorizer roleAuthorizer
    )
        => (_now, _reportsService, _roleAuthorizer) = (now, reportsService, roleAuthorizer);

    public async Task<ReportMetaData> Handle(GetMetaDataQuery request, CancellationToken cancellationToken)
    {
        _roleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Admin };
        _roleAuthorizer.Authorize();

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
