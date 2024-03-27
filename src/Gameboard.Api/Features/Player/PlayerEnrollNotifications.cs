using MediatR;

namespace Gameboard.Api.Features.Players;

public sealed class PlayerEnrollNotificationContext
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string GameId { get; set; }
    public required string TeamId { get; set; }
    public required string UserId { get; set; }
}

public record PlayerEnrolledNotification(PlayerEnrollNotificationContext Context) : INotification;
public record PlayerUnenrolledNotification(PlayerEnrollNotificationContext Context) : INotification;
