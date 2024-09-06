using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.SystemNotifications;

public record DeleteSystemNotificationCommand(string SystemNotificationId) : IRequest;

internal class DeleteSystemNotificationHandler(
    EntityExistsValidator<DeleteSystemNotificationCommand, SystemNotification> notificationExists,
    IStore store,
    IValidatorService<DeleteSystemNotificationCommand> validatorService
    ) : IRequestHandler<DeleteSystemNotificationCommand>
{
    private readonly EntityExistsValidator<DeleteSystemNotificationCommand, SystemNotification> _notificationExists = notificationExists;
    private readonly IStore _store = store;
    private readonly IValidatorService<DeleteSystemNotificationCommand> _validatorService = validatorService;

    public async Task Handle(DeleteSystemNotificationCommand request, CancellationToken cancellationToken)
    {
        // validate/authorize
        await _validatorService
            .Auth(a => a.RequirePermissions(PermissionKey.SystemNotifications_CreateEdit))
            .AddValidator(_notificationExists.UseProperty(r => r.SystemNotificationId))
            .Validate(request, cancellationToken);

        // now do the thing
        await _store
            .WithNoTracking<SystemNotification>()
            .Where(n => n.Id == request.SystemNotificationId)
            .ExecuteUpdateAsync(up => up.SetProperty(n => n.IsDeleted, true), cancellationToken);
    }
}
