using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
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

    public CreateSystemNotificationHandler
    (
        IActingUserService actingUserService,
        IStore store,
        ISystemNotificationsService systemNotificationsService,
        UserRoleAuthorizer userRoleAuthorizer
    )
    {
        _actingUserService = actingUserService;
        _store = store;
        _systemNotificationsService = systemNotificationsService;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<ViewSystemNotification> Handle(CreateSystemNotificationCommand request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin)
            .Authorize();

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
                CreatedByUserId = _actingUserService.Get().Id
            });

        return await _systemNotificationsService.Get(created.Id);
    }
}
