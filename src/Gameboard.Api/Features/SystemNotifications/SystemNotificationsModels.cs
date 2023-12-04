using System;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.SystemNotifications;

public sealed class CreateSystemNotification
{
    public required string Title { get; set; }
    public required string MarkdownContent { get; set; }
    public DateTimeOffset? StartsOn { get; set; }
    public DateTimeOffset? EndsOn { get; set; }
    public SystemNotificationType? NotificationType { get; set; }
}

public class ViewSystemNotification
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required string MarkdownContent { get; set; }
    public required DateTimeOffset? StartsOn { get; set; }
    public required DateTimeOffset? EndsOn { get; set; }
    public required SystemNotificationType NotificationType { get; set; }
    public required SimpleEntity CreatedBy { get; set; }
}

public sealed class AdminViewSystemNotification : ViewSystemNotification
{
    public required int CalloutViewCount { get; set; }
    public required int FullViewCount { get; set; }
}
