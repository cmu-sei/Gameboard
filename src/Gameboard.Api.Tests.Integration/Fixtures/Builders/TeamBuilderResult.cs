namespace Gameboard.Api.Tests.Integration.Fixtures;

public class TeamBuilderResult
{
    public required string TeamId { get; set; }
    public required Data.Game Game { get; set; }
    public required Data.Player Manager { get; set; }
}
