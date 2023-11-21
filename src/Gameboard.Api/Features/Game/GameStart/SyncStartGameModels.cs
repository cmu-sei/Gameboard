using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Games.Start;

public sealed class ValidateSyncStartResult
{
    public required bool CanStart { get; set; }
    public required Data.Game Game { get; set; }
    public required bool IsStarted { get; set; }
    public required IEnumerable<ValidateSyncStartResultPlayer> Players { get; set; }
    public required SyncStartState SyncStartState { get; set; }
}

public sealed class ValidateSyncStartResultPlayer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool HasChallenges { get; set; }
    public required DateTimeOffset? SessionBegin { get; set; }
    public required DateTimeOffset? SessionEnd { get; set; }
    public required string TeamId { get; set; }
}
