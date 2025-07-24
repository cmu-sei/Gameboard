namespace Gameboard.Api.Features.Practice;

public record ListGamesResponse(ListGamesResponseGame[] Games);

public sealed class ListGamesResponseGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required int ChallengeCount { get; set; }
}
