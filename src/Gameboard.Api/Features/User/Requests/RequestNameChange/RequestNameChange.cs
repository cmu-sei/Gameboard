using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Users;

public record RequestNameChangeCommand(string UserId, string RequestedName) : IRequest<RequestNameChangeResponse>;

internal sealed class RequestNameChangeHandler
(
    CoreOptions coreOptions,
    IUserRolePermissionsService permissionsService,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<RequestNameChangeCommand, RequestNameChangeResponse>
{
    private readonly CoreOptions _coreOptions = coreOptions;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<RequestNameChangeResponse> Handle(RequestNameChangeCommand request, CancellationToken cancellationToken)
    {
        await _validatorService.Auth
        (
            config =>
                config
                    .RequirePermissions(PermissionKey.Teams_ApproveNameChanges)
                    .UnlessUserIdIn(request.UserId)
        )
        .AddValidator(request.RequestedName.IsEmpty(), new MissingRequiredInput<string>(nameof(request.RequestedName), request.RequestedName))
        .AddValidator(async () => !_coreOptions.NameChangesAllow && !await _permissionsService.Can(PermissionKey.Teams_ApproveNameChanges), new NameChangesDisabled())
        .Validate(cancellationToken);

        var currentUser = await _store
            .WithNoTracking<Data.User>()
            .Where(u => u.Id == request.UserId)
            .Select(u => new
            {
                u.ApprovedName
            })
            .SingleAsync(cancellationToken);

        var canRenameWithoutApproval = currentUser.ApprovedName == request.RequestedName || !_coreOptions.NameChangesRequireApproval || await _permissionsService.Can(PermissionKey.Teams_ApproveNameChanges);
        var finalStatus = canRenameWithoutApproval ? string.Empty : "pending";

        await _store
            .WithNoTracking<Data.User>()
            .Where(u => u.Id == request.UserId)
            .ExecuteUpdateAsync
            (
                up => up
                    // if they're allowed to ignore approval, directly set the approved name
                    .SetProperty(u => u.ApprovedName, u => canRenameWithoutApproval ? request.RequestedName.Trim() : u.ApprovedName)
                    // if they've been hard-renamed, clear the request
                    .SetProperty(u => u.Name, canRenameWithoutApproval ? string.Empty : request.RequestedName.Trim())
                    .SetProperty(u => u.NameStatus, finalStatus),
                cancellationToken
            );

        return new RequestNameChangeResponse
        {
            Name = request.RequestedName,
            Status = finalStatus
        };
    }
}
