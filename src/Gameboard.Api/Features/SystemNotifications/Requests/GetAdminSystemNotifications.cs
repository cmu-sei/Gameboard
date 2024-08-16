using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.SystemNotifications;

public record GetAdminSystemNotificationsQuery() : IRequest<IEnumerable<AdminViewSystemNotification>>;

internal class GetAdminSystemNotificationsHandler(IStore store, IValidatorService validatorService) : IRequestHandler<GetAdminSystemNotificationsQuery, IEnumerable<AdminViewSystemNotification>>
{
    private readonly IStore _store = store;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<IEnumerable<AdminViewSystemNotification>> Handle(GetAdminSystemNotificationsQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .ConfigureAuthorization(a => a.RequirePermissions(UserRolePermissionKey.SystemNotifications_CreateEdit))
            .Validate(cancellationToken);

        return await _store
            .WithNoTracking<SystemNotification>()
            .Where(n => !n.IsDeleted)
            .Select(n => new AdminViewSystemNotification
            {
                Id = n.Id,
                Title = n.Title,
                IsDismissible = n.IsDismissible,
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
