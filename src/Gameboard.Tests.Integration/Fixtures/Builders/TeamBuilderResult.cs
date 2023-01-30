using Gameboard.Api;

namespace Gameboard.Tests.Integration.Fixtures;

public class TeamBuilderResult
{
    public required string TeamId { get; set; }
    public Api.Data.Challenge? Challenge { get; set; }
    public required Api.Data.Player Manager { get; set; }
    public required IEnumerable<Api.Data.Player> Players { get; set; }
}
