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
    public required bool HasStartedSession { get; set; }
    public required bool IsReady { get; set; }
}

public class SyncStartState
{
    public required SimpleEntity Game { get; set; }
    public required IEnumerable<SyncStartTeam> Teams { get; set; }
    public required bool AllSessionsStarted { get; set; }
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
    public IDictionary<string, IEnumerable<SyncStartGameStartedStatePlayer>> Teams { get; set; }
}

public class SyncStartGameStartedStatePlayer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string UserId { get; set; }
}

public sealed class UpdateIsReadyRequest
{
    public required bool IsReady { get; set; }
}

public sealed class ValidateSyncStartResult
{
    public required ValidateSyncStartGame Game { get; set; }
    public required bool AllPlayersReady { get; set; }
    public required bool CanStart { get; set; }
    public required bool HasStartedPlayers { get; set; }
    public required bool IsInExecutionWindow { get; set; }
    public required IEnumerable<ValidateSyncStartResultPlayer> Players { get; set; }
    public required SyncStartState SyncStartState { get; set; }
}

public sealed class ValidateSyncStartGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsSyncStart { get; set; }
    public required int SessionMinutes { get; set; }
    public required DateRange ExecutionWindow { get; set; }
}

public sealed class ValidateSyncStartResultPlayer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsReady { get; set; }
    public required DateTimeOffset? SessionBegin { get; set; }
    public required DateTimeOffset? SessionEnd { get; set; }
    public required string TeamId { get; set; }
    public required string UserId { get; set; }
}
