using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.SystemNotifications;

public record CreateSystemNotificationCommand(CreateSystemNotification Create) : IRequest<ViewSystemNotification>;

internal class CreateSystemNotificationHandler : IRequestHandler<CreateSystemNotificationCommand, ViewSystemNotification>
{
    private readonly IActingUserService _actingUserService;
    private readonly IStore _store;
    private readonly ISystemNotificationsService _systemNotificationsService;
    private readonly IValidatorService<CreateSystemNotificationCommand> _validatorService;

    public CreateSystemNotificationHandler
    (
        IActingUserService actingUserService,
        IStore store,
        ISystemNotificationsService systemNotificationsService,
        IValidatorService<CreateSystemNotificationCommand> validatorService
    )
    {
        _actingUserService = actingUserService;
        _store = store;
        _systemNotificationsService = systemNotificationsService;
        _validatorService = validatorService;
    }

    public async Task<ViewSystemNotification> Handle(CreateSystemNotificationCommand request, CancellationToken cancellationToken)
    {
        // validate
        await _validatorService
            .ConfigureAuthorization(c => c.RequirePermissions(Users.PermissionKey.SystemNotifications_CreateEdit))
            .AddValidator
            (
                (req, ctx) =>
                {
                    if (request.Create.Title.IsEmpty())
                        ctx.AddValidationException(new MissingRequiredInput<CreateSystemNotificationCommand>(nameof(request.Create.Title), request));

                    if (request.Create.MarkdownContent.IsEmpty())
                        ctx.AddValidationException(new MissingRequiredInput<CreateSystemNotificationCommand>(nameof(request.Create.MarkdownContent), request));
                }
            )
            .Validate(request, cancellationToken);

        var created = await _store
            .Create(new SystemNotification
            {
                Title = request.Create.Title,
                MarkdownContent = request.Create.MarkdownContent,
                StartsOn = request.Create.StartsOn,
                EndsOn = request.Create.EndsOn,
                NotificationType = request.Create.NotificationType.GetValueOrDefault() is not default(SystemNotificationType) ?
                    request.Create.NotificationType.Value :
                    SystemNotificationType.GeneralInfo,
                CreatedByUserId = _actingUserService.Get().Id,
                IsDeleted = false,
                IsDismissible = request.Create.IsDismissible ?? true
            });

        return await _systemNotificationsService.Get(created.Id);
    }
}
