using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.SystemNotifications;

public record GetVisibleNotificationsQuery() : IRequest<IEnumerable<ViewSystemNotification>>;

internal class GetVisibleNotificationsHandler(
    IActingUserService actingUserService,
    INowService now,
    IStore store,
    ISystemNotificationsService systemNotificationsService,
    IValidatorService validatorService
    ) : IRequestHandler<GetVisibleNotificationsQuery, IEnumerable<ViewSystemNotification>>
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly INowService _now = now;
    private readonly IStore _store = store;
    private readonly ISystemNotificationsService _systemNotificationService = systemNotificationsService;
    private readonly IValidatorService _validatorService = validatorService;

    public async Task<IEnumerable<ViewSystemNotification>> Handle(GetVisibleNotificationsQuery request, CancellationToken cancellationToken)
    {
        await _validatorService
            .Auth(a => a.RequireAuthentication())
            .Validate(cancellationToken);

        var nowish = _now.Get();
        var actingUserId = _actingUserService.Get().Id;

        return await _store
            .WithNoTracking<SystemNotification>()
            .Where(n => !n.IsDeleted)
            .Where(n => n.StartsOn == null || nowish > n.StartsOn)
            .Where(n => n.EndsOn == null || nowish < n.EndsOn)
            .Where
            (
                n =>
                    !n.IsDismissible ||
                    (
                        !n.Interactions.Any(i => i.UserId == actingUserId && i.DismissedOn != null) &&
                        !n.Interactions.Any(i => i.UserId == actingUserId && i.SawFullNotificationOn != null)
                    )
            )
            .Select(entity => new ViewSystemNotification
            {
                Id = entity.Id,
                Title = entity.Title,
                IsDismissible = entity.IsDismissible,
                MarkdownContent = entity.MarkdownContent,
                StartsOn = entity.StartsOn,
                EndsOn = entity.EndsOn,
                NotificationType = entity.NotificationType,
                CreatedBy = new SimpleEntity { Id = entity.CreatedByUserId, Name = entity.CreatedByUser.ApprovedName }
            })
            .ToArrayAsync(cancellationToken);
    }
}
