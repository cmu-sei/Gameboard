using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.SystemNotifications;

public record GetVisibleNotificationsQuery() : IRequest<IEnumerable<ViewSystemNotification>>;

internal class GetVisibleNotificationsHandler : IRequestHandler<GetVisibleNotificationsQuery, IEnumerable<ViewSystemNotification>>
{
    private readonly IActingUserService _actingUserService;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ISystemNotificationsService _systemNotificationService;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;

    public GetVisibleNotificationsHandler
    (
        IActingUserService actingUserService,
        INowService now,
        IStore store,
        ISystemNotificationsService systemNotificationsService,
        UserRoleAuthorizer userRoleAuthorizer
    )
    {
        _actingUserService = actingUserService;
        _now = now;
        _store = store;
        _systemNotificationService = systemNotificationsService;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<IEnumerable<ViewSystemNotification>> Handle(GetVisibleNotificationsQuery request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
           .AllowRoles(UserRole.Member)
           .Authorize();

        var nowish = _now.Get();
        var actingUserId = _actingUserService.Get().Id;

        return await _store
            .WithNoTracking<SystemNotification>()
            .Where(n => n.StartsOn == null || nowish > n.StartsOn)
            .Where(n => n.EndsOn == null || nowish < n.EndsOn)
            .Where(n => !n.Interactions.Any(i => i.UserId == actingUserId && i.DismissedOn != null))
            .Select(n => _systemNotificationService.ToViewSystemNotification(n))
            .ToArrayAsync(cancellationToken);
    }
}
