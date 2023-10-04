using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;

namespace Gameboard.Api.Features.Reports;

internal class ReportsQueryValidator : IGameboardRequestValidator<IReportQuery>
{
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public ReportsQueryValidator(UserRoleAuthorizer roleAuthorizer)
    {
        _roleAuthorizer = roleAuthorizer;
    }

    public Task Validate(IReportQuery request, CancellationToken cancellationToken)
    {
        _roleAuthorizer.AllowRoles(UserRole.Admin, UserRole.Registrar, UserRole.Support);
        _roleAuthorizer.Authorize();

        return Task.CompletedTask;
    }
}
