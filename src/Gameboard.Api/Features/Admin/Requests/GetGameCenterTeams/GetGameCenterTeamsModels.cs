using System;
using System.Collections.Generic;
using Microsoft.Identity.Client;

namespace Gameboard.Api.Features.Admin;

public enum GetGameCenterTeamsSort
{
    Rank,
    TeamName,
    TimeRemaining,
    TimeSinceStart
}

public sealed class GetGameCenterTeamsArgs
{
    public bool? HasScored { get; set; }
    public PlayerMode? PlayerMode { get; set; }
    public string Search { get; set; }
    public GameCenterTeamsStatus? Status { get; set; }

    // page and sort
    public int? PageNumber { get; set; }
    public GetGameCenterTeamsSort? Sort { get; set; }
    public SortDirection? SortDirection { get; set; }
}

public sealed class GameCenterTeamsResults
{
    public required PagedEnumerable<GameCenterTeamsResultsTeam> Teams { get; set; }
}

public sealed class GameCenterTeamsResultsTeam
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    public required GameCenterTeamsPlayer Captain { get; set; }
    public required int ChallengesCompleteCount { get; set; }
    public required int ChallengesPartialCount { get; set; }
    public required int ChallengesRemainingCount { get; set; }
    public required bool IsExtended { get; set; }
    public required bool? IsReady { get; set; }
    public required IEnumerable<GameCenterTeamsPlayer> Players { get; set; }
    public required int? Rank { get; set; }
    public required DateTimeOffset RegisteredOn { get; set; }
    public required GameCenterTeamsSession Session { get; set; }
    public required int TicketCount { get; set; }
}

public enum GameCenterTeamsStatus
{
    Complete,
    NotStarted,
    Playing
}

public sealed class GameCenterTeamsPlayer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsReady { get; set; }
    public required SimpleSponsor Sponsor { get; set; }
}

public sealed class GameCenterTeamsSession
{
    public required long? Start { get; set; }
    public required long? End { get; set; }
    public required double? TimeRemainingMs { get; set; }
    public required double? TimeSinceStartMs { get; set; }
}
