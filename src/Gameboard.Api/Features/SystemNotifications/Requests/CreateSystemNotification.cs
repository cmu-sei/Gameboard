using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;

namespace Gameboard.Api.Features.SystemNotifications;

public record CreateSystemNotificationCommand(CreateSystemNotification Create) : IRequest<ViewSystemNotification>;

internal class CreateSystemNotificationHandler : IRequestHandler<CreateSystemNotificationCommand, ViewSystemNotification>
{
    private readonly IActingUserService _actingUserService;
    private readonly IStore _store;
    private readonly ISystemNotificationsService _systemNotificationsService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<CreateSystemNotificationCommand> _validatorService;

    public CreateSystemNotificationHandler
    (
        IActingUserService actingUserService,
        IStore store,
        ISystemNotificationsService systemNotificationsService,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<CreateSystemNotificationCommand> validatorService
    )
    {
        _actingUserService = actingUserService;
        _store = store;
        _systemNotificationsService = systemNotificationsService;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validatorService = validatorService;
    }

    public async Task<ViewSystemNotification> Handle(CreateSystemNotificationCommand request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin)
            .Authorize();

        _validatorService.AddValidator
        (
            (req, ctx) =>
            {
                if (request.Create.Title.IsEmpty())
                    ctx.AddValidationException(new MissingRequiredInput<CreateSystemNotificationCommand>(nameof(request.Create.Title), request));

                if (request.Create.MarkdownContent.IsEmpty())
                    ctx.AddValidationException(new MissingRequiredInput<CreateSystemNotificationCommand>(nameof(request.Create.MarkdownContent), request));
            }
        );

        await _validatorService.Validate(request, cancellationToken);

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
                IsDeleted = false
            });

        return await _systemNotificationsService.Get(created.Id);
    }
}
