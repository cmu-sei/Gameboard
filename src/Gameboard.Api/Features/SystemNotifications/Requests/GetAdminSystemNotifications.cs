using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.SystemNotifications;

public record GetAdminSystemNotificationsQuery() : IRequest<IEnumerable<AdminViewSystemNotification>>;

internal class GetAdminSystemNotificationsHandler : IRequestHandler<GetAdminSystemNotificationsQuery, IEnumerable<AdminViewSystemNotification>>
{
    private readonly IStore _store;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;

    public GetAdminSystemNotificationsHandler(IStore store, UserRoleAuthorizer userRoleAuthorizer)
    {
        _store = store;
        _userRoleAuthorizer = userRoleAuthorizer;
    }

    public async Task<IEnumerable<AdminViewSystemNotification>> Handle(GetAdminSystemNotificationsQuery request, CancellationToken cancellationToken)
    {
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin)
            .Authorize();

        return await _store
            .WithNoTracking<SystemNotification>()
                .Include(n => n.Interactions)
                .Include(n => n.CreatedByUser)
            .Where(n => !n.IsDeleted)
            .Select(n => new AdminViewSystemNotification
            {
                Id = n.Id,
                Title = n.Title,
                MarkdownContent = n.MarkdownContent,
                StartsOn = n.StartsOn,
                EndsOn = n.EndsOn,
                NotificationType = n.NotificationType,
                CreatedBy = new SimpleEntity { Id = n.CreatedByUserId, Name = n.CreatedByUser.ApprovedName },
                CalloutViewCount = n.Interactions.Where(i => i.SawCalloutOn != null).Count(),
                FullViewCount = n.Interactions.Where(i => i.SawFullNotificationOn != null).Count()
            })
            .OrderBy(n => n.StartsOn)
                .ThenBy(n => n.EndsOn)
            .ToArrayAsync(cancellationToken);
    }
}
