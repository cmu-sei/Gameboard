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

public record UpdateSystemNotificationCommand(UpdateSystemNotificationRequest Update) : IRequest<ViewSystemNotification>;

internal class UpdateSystemNotificationHandler(
    EntityExistsValidator<UpdateSystemNotificationCommand, SystemNotification> notificationExists,
    IStore store,
    ISystemNotificationsService systemNotificationsService,
    IValidatorService<UpdateSystemNotificationCommand> validatorService
    ) : IRequestHandler<UpdateSystemNotificationCommand, ViewSystemNotification>
{
    private readonly EntityExistsValidator<UpdateSystemNotificationCommand, SystemNotification> _notificationExists = notificationExists;
    private readonly IStore _store = store;
    private readonly ISystemNotificationsService _systemNotificationsService = systemNotificationsService;
    private readonly IValidatorService<UpdateSystemNotificationCommand> _validatorService = validatorService;

    public async Task<ViewSystemNotification> Handle(UpdateSystemNotificationCommand request, CancellationToken cancellationToken)
    {
        await _validatorService
            .ConfigureAuthorization(a => a.RequirePermissions(UserRolePermissionKey.SystemNotifications_CreateEdit))
            .AddValidator(_notificationExists.UseProperty(r => r.Update.Id))
            .AddValidator
            (
                (req, ctx) =>
                {
                    if (request.Update.Title.IsEmpty())
                        ctx.AddValidationException(new MissingRequiredInput<UpdateSystemNotificationCommand>(nameof(request.Update.Title), request));

                    if (request.Update.MarkdownContent.IsEmpty())
                        ctx.AddValidationException(new MissingRequiredInput<UpdateSystemNotificationCommand>(nameof(request.Update.MarkdownContent), request));
                }
            )
            .Validate(request, cancellationToken);

        await _store
            .WithNoTracking<SystemNotification>()
            .Where(n => n.Id == request.Update.Id && !n.IsDeleted)
            .ExecuteUpdateAsync
            (
                up => up
                    .SetProperty(n => n.Title, request.Update.Title)
                    .SetProperty(n => n.MarkdownContent, request.Update.MarkdownContent)
                    .SetProperty(n => n.IsDismissible, request.Update.IsDismissible)
                    .SetProperty(n => n.StartsOn, request.Update.StartsOn)
                    .SetProperty(n => n.EndsOn, request.Update.EndsOn)
                    .SetProperty(n => n.NotificationType, request.Update.NotificationType)
                , cancellationToken
            );

        return await _systemNotificationsService.Get(request.Update.Id);
    }
}
