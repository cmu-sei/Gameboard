namespace Gameboard.Api.Hubs;

public enum GameboardHubType
{
    Game,
    Score,
    Support,
    Team,
    User
}

public class GameboardHubUserConnection
{
    public required string ConnectionId { get; set; }
    public required string UserId { get; set; }
}
