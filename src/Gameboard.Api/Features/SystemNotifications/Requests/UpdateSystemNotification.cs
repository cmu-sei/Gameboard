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

public record UpdateSystemNotificationCommand(UpdateSystemNotificationRequest Update) : IRequest<ViewSystemNotification>;

internal class UpdateSystemNotificationHandler : IRequestHandler<UpdateSystemNotificationCommand, ViewSystemNotification>
{
    private readonly EntityExistsValidator<UpdateSystemNotificationCommand, SystemNotification> _notificationExists;
    private readonly IStore _store;
    private readonly ISystemNotificationsService _systemNotificationsService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<UpdateSystemNotificationCommand> _validatorService;

    public UpdateSystemNotificationHandler
    (
        EntityExistsValidator<UpdateSystemNotificationCommand, SystemNotification> notificationExists,
        IStore store,
        ISystemNotificationsService systemNotificationsService,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<UpdateSystemNotificationCommand> validatorService
    )
    {
        _notificationExists = notificationExists;
        _store = store;
        _systemNotificationsService = systemNotificationsService;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task<ViewSystemNotification> Handle(UpdateSystemNotificationCommand request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin)
            .Authorize();

        await _validatorService
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
            .Where(n => n.Id == request.Update.Id)
            .ExecuteUpdateAsync
            (
                up => up
                    .SetProperty(n => n.Title, request.Update.Title)
                    .SetProperty(n => n.MarkdownContent, request.Update.MarkdownContent)
                    .SetProperty(n => n.StartsOn, request.Update.StartsOn)
                    .SetProperty(n => n.EndsOn, request.Update.EndsOn)
                    .SetProperty(n => n.NotificationType, request.Update.NotificationType)
                , cancellationToken
            );

        return await _systemNotificationsService.Get(request.Update.Id);
    }
}
