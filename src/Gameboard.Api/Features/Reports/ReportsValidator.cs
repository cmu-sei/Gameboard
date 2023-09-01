using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;

namespace Gameboard.Api.Features.Reports;

internal class ReportsQueryValidator<T> : IGameboardRequestValidator<IReportQuery>
{
    private readonly UserRoleAuthorizer _roleAuthorizer;

    public ReportsQueryValidator(UserRoleAuthorizer roleAuthorizer)
    {
        _roleAuthorizer = roleAuthorizer;
    }

    public Task Validate(IReportQuery request)
    {
        _roleAuthorizer.AllowedRoles = new UserRole[] { UserRole.Director, UserRole.Admin, UserRole.Support };
        _roleAuthorizer.Authorize();

        return Task.CompletedTask;
    }
}
