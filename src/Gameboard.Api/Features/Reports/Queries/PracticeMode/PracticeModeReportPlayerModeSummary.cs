using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record PracticeModeReportPlayerModeSummaryQuery(string UserId, bool IsPractice) : IRequest<PracticeModeReportPlayerModeSummary>;

internal class PracticeModeReportPlayerModeSummaryHandler : IRequestHandler<PracticeModeReportPlayerModeSummaryQuery, PracticeModeReportPlayerModeSummary>
{
    private readonly IPracticeModeReportService _reportService;
    private readonly EntityExistsValidator<PracticeModeReportPlayerModeSummaryQuery, Data.User> _userExists;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<PracticeModeReportPlayerModeSummaryQuery> _validatorService;

    public PracticeModeReportPlayerModeSummaryHandler
    (
        IPracticeModeReportService reportService,
        EntityExistsValidator<PracticeModeReportPlayerModeSummaryQuery, Data.User> userExists,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<PracticeModeReportPlayerModeSummaryQuery> validatorService
    ) => (_reportService, _userExists, _userRoleAuthorizer, _validatorService) = (reportService, userExists, userRoleAuthorizer, validatorService);

    public async Task<PracticeModeReportPlayerModeSummary> Handle(PracticeModeReportPlayerModeSummaryQuery request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Director, UserRole.Support)
            .Authorize();

        _validatorService.AddValidator(_userExists.UseProperty(r => r.UserId));
        await _validatorService.Validate(request, cancellationToken);

        var result = await _reportService.GetPlayerModePerformanceSummary(request.UserId, request.IsPractice, cancellationToken);
        return result;
    }
}
