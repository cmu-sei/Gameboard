using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Users;

public interface IUserHubBus
{
    Task SendAnnouncement(UserHubAnnouncementEvent ev);
}

internal sealed class UserHubBus : IUserHubBus, IGameboardHubService
{
    private readonly IHubContext<UserHub, IUserHubEvent> _hubContext;
    private readonly IStore _store;

    public GameboardHubType GroupType => GameboardHubType.User;

    public UserHubBus(IHubContext<UserHub, IUserHubEvent> hubContext, IStore store)
        => (_hubContext, _store) = (hubContext, store);

    public async Task SendAnnouncement(UserHubAnnouncementEvent ev)
    {
        var eventData = new UserHubEvent<UserHubAnnouncementEvent>
        {
            Data = ev,
            EventType = UserHubEventType.AnnouncementGlobal
        };

        if (ev.TeamId.IsEmpty())
        {
            await _hubContext
                .Clients
                .All
                .Announcement(eventData);
        }
        else
        {
            var userIds = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.TeamId == ev.TeamId)
                .Select(p => p.UserId)
                .Distinct()
                .ToArrayAsync();

            await _hubContext
                .Clients
                .Users(userIds)
                .Announcement(eventData);
        }
    }
}
