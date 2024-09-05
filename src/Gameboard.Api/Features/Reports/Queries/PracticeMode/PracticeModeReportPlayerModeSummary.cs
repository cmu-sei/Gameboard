using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record PracticeModeReportPlayerModeSummaryQuery(string UserId, bool IsPractice) : IRequest<PracticeModeReportPlayerModeSummary>;

internal class PracticeModeReportPlayerModeSummaryHandler(IPracticeModeReportService reportService, EntityExistsValidator<PracticeModeReportPlayerModeSummaryQuery, Data.User> userExists, IValidatorService<PracticeModeReportPlayerModeSummaryQuery> validatorService) : IRequestHandler<PracticeModeReportPlayerModeSummaryQuery, PracticeModeReportPlayerModeSummary>
{
    private readonly IPracticeModeReportService _reportService = reportService;
    private readonly EntityExistsValidator<PracticeModeReportPlayerModeSummaryQuery, Data.User> _userExists = userExists;
    private readonly IValidatorService<PracticeModeReportPlayerModeSummaryQuery> _validatorService = validatorService;

    public async Task<PracticeModeReportPlayerModeSummary> Handle(PracticeModeReportPlayerModeSummaryQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(c => c.RequirePermissions(PermissionKey.Reports_View))
            .AddValidator(_userExists.UseProperty(r => r.UserId))
            .Validate(request, cancellationToken);

        var result = await _reportService.GetPlayerModePerformanceSummary(request.UserId, request.IsPractice, cancellationToken);
        return result;
    }
}
