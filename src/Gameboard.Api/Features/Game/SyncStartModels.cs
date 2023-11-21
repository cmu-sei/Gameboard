using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Games;

public class SyncStartPlayer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsReady { get; set; }
}

public class SyncStartPlayerStatusUpdate
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string GameId { get; set; }
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

public class SyncStartGameSession
{
    public required DateTimeOffset SessionBegin { get; set; }
    public required DateTimeOffset SessionEnd { get; set; }
}

public class SyncStartGameStartedState
{
    public required SimpleEntity Game { get; set; }
    public required DateTimeOffset SessionBegin { get; set; }
    public required DateTimeOffset SessionEnd { get; set; }
    public IDictionary<string, IEnumerable<SimpleEntity>> Teams { get; set; }
}

public sealed class UpdateIsReadyModel
{
    public required bool IsReady { get; set; }
}
