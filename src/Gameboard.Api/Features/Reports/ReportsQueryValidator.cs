using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;

namespace Gameboard.Api.Features.Reports;

internal class ReportsQueryValidator(IValidatorService<IReportQuery> validatorService) : IGameboardRequestValidator<IReportQuery>
{
    private readonly IValidatorService<IReportQuery> _validatorService = validatorService;

    public async Task Validate(IReportQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .ConfigureAuthorization(config => config.RequirePermissions(UserRolePermissionKey.Reports_View))
            .Validate(request, cancellationToken);
    }
}
