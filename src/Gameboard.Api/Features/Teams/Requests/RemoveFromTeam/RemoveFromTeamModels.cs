namespace Gameboard.Api.Features.Teams;

public record RemoveFromTeamResponse
{
    public required SimpleEntity Game { get; set; }
    public required SimpleEntity Player { get; set; }
    public required string TeamId { get; set; }
    public required SimpleEntity UserId { get; set; }
}
