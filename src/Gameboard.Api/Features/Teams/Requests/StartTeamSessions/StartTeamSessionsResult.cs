using System.Collections.Generic;

namespace Gameboard.Api.Features.Teams;

public sealed class StartTeamSessionsResult
{
    public required IDictionary<string, StartTeamSessionsResultTeam> Teams { get; set; }
}

public sealed class StartTeamSessionsResultTeam
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool ResourcesDeploying { get; set; }
    public required CalculatedSessionWindow SessionWindow { get; set; }
    public required SimpleEntity Captain { get; set; }
    public required IEnumerable<SimpleEntity> Players { get; set; }
}
