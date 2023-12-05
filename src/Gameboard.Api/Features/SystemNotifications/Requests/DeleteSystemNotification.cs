using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.SystemNotifications;

public record DeleteSystemNotificationCommand(string SystemNotificationId) : IRequest;

internal class DeleteSystemNotificationHandler : IRequestHandler<DeleteSystemNotificationCommand>
{
    private readonly EntityExistsValidator<DeleteSystemNotificationCommand, SystemNotification> _notificationExists;
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<DeleteSystemNotificationCommand> _validatorService;

    public DeleteSystemNotificationHandler
    (
        EntityExistsValidator<DeleteSystemNotificationCommand, SystemNotification> notificationExists,
        IStore store,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<DeleteSystemNotificationCommand> validatorService
    )
    {
        _notificationExists = notificationExists;
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task Handle(DeleteSystemNotificationCommand request, CancellationToken cancellationToken)
    {
        // validate/authorize
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin)
            .Authorize();

        await _validatorService
            .AddValidator(_notificationExists.UseProperty(r => r.SystemNotificationId))
            .Validate(request, cancellationToken);

        // now do the thing
        await _store
            .WithNoTracking<SystemNotification>()
            .Where(n => n.Id == request.SystemNotificationId)
            .ExecuteUpdateAsync(up => up.SetProperty(n => n.IsDeleted, true), cancellationToken);
    }
}
