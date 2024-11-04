using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Users;

public record RequestNameChangeCommand(string UserId, RequestNameChangeRequest Request) : IRequest<RequestNameChangeResponse>;

internal sealed class RequestNameChangeHandler
(
    IActingUserService actingUserService,
    CoreOptions coreOptions,
    IUserRolePermissionsService permissionsService,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<RequestNameChangeCommand, RequestNameChangeResponse>
{
    private readonly IActingUserService _actingUserService = actingUserService;
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
                    .Unless(() => Task.FromResult(request.UserId == _actingUserService.Get()?.Id && _coreOptions.NameChangeIsEnabled))
        )
        .AddValidator(request.Request.RequestedName.IsEmpty(), new MissingRequiredInput<string>(nameof(request.Request.RequestedName), request.Request.RequestedName))
        .AddValidator(async () => !_coreOptions.NameChangeIsEnabled && !await _permissionsService.Can(PermissionKey.Teams_ApproveNameChanges), new NameChangesDisabled())
        .Validate(cancellationToken);

        var currentUser = await _store
            .WithNoTracking<Data.User>()
            .Where(u => u.Id == request.UserId)
            .Select(u => new
            {
                u.ApprovedName
            })
            .SingleAsync(cancellationToken);

        var isSuperPowerfulRenameAdmin = await _permissionsService.Can(PermissionKey.Teams_ApproveNameChanges);
        var canRenameWithoutApproval = isSuperPowerfulRenameAdmin || currentUser.ApprovedName == request.Request.RequestedName || !_coreOptions.NameChangeRequiresApproval;

        // COMPUTING THE STATUS IS CURRENTLY WEIRD:
        // admins hitting this endpoint can hard-set a status IF the request is being rejected (for example, to inform the user that the name isn't allowed because it discloses PII)
        // everyone else just has the requested status ignored and computed instead
        var finalStatus = string.Empty;
        if (currentUser.ApprovedName == request.Request.RequestedName)
        {
            finalStatus = request.Request.Status ?? string.Empty;
        }
        else
        {
            finalStatus = canRenameWithoutApproval ? string.Empty : "pending";
        }

        await _store
            .WithNoTracking<Data.User>()
            .Where(u => u.Id == request.UserId)
            .ExecuteUpdateAsync
            (
                up => up
                    // if they're allowed to ignore approval, directly set the approved name
                    .SetProperty(u => u.ApprovedName, u => canRenameWithoutApproval ? request.Request.RequestedName.Trim() : u.ApprovedName)
                    // if they've been hard-renamed, clear the request
                    .SetProperty(u => u.Name, canRenameWithoutApproval ? string.Empty : request.Request.RequestedName.Trim())
                    .SetProperty(u => u.NameStatus, finalStatus),
                cancellationToken
            );

        return new RequestNameChangeResponse
        {
            Name = request.Request.RequestedName,
            Status = finalStatus
        };
    }
}
