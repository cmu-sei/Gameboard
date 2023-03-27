using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Games;

public class SyncStartPlayer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsReady { get; set; }
}

public class SyncStartTeam
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required IEnumerable<SyncStartPlayer> Players { get; set; }
    public required bool IsReady { get; set; }
}

public class SyncStartState
{
    public required SimpleEntity Game { get; set; }
    public required IEnumerable<SyncStartTeam> Teams { get; set; }
    public required bool IsReady { get; set; }
}
