using System.Threading.Tasks;

namespace Gameboard.Api.Features.Users;

public enum UserHubEventType
{
    AnnouncementGlobal
}

public class UserHubEvent<TData> where TData : class
{
    public required TData Data { get; set; }
    public required UserHubEventType EventType { get; set; }
}

public interface IUserHubEvent
{
    Task Announcement(UserHubEvent<UserHubAnnouncementEvent> ev);
}

public sealed class UserHubAnnouncementEvent
{
    public required SimpleEntity SentByUser { get; set; }
    public required string TeamId { get; set; }
    public required string Title { get; set; }
    public required string ContentMarkdown { get; set; }
}
