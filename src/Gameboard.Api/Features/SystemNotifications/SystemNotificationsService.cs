using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.SystemNotifications;

public interface ISystemNotificationsService
{
    Task<ViewSystemNotification> Get(string id);
    ViewSystemNotification ToViewSystemNotification(SystemNotification notification);
}

internal class SystemNotificationsService : ISystemNotificationsService
{
    private readonly IStore _store;

    public SystemNotificationsService(IStore store)
    {
        _store = store;
    }

    public Task<ViewSystemNotification> Get(string id)
        => _store
            .WithNoTracking<SystemNotification>()
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
            .SingleOrDefaultAsync(n => n.Id == id);

    public ViewSystemNotification ToViewSystemNotification(SystemNotification notification)
        => new()
        {
            Id = notification.Id,
            Title = notification.Title,
            IsDismissible = notification.IsDismissible,
            MarkdownContent = notification.MarkdownContent,
            StartsOn = notification.StartsOn,
            EndsOn = notification.EndsOn,
            NotificationType = notification.NotificationType,
            CreatedBy = new SimpleEntity { Id = notification.CreatedByUserId, Name = notification.CreatedByUser.ApprovedName }
        };
}
