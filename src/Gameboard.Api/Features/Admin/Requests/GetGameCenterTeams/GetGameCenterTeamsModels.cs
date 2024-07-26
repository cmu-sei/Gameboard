using System.Collections.Generic;
using Gameboard.Api.Features.Scores;

namespace Gameboard.Api.Features.Admin;

public enum GetGameCenterTeamsAdvancementFilter
{
    AdvancedFromPreviousGame,
    AdvancedToNextGame
}

public enum GetGameCenterTeamsSort
{
    Rank,
    TeamName,
    TimeRemaining,
    TimeSinceStart
}

public sealed class GetGameCenterTeamsArgs
{
    public GetGameCenterTeamsAdvancementFilter? Advancement { get; set; }
    public bool? HasPendingNames { get; set; }
    public bool? HasScored { get; set; }
    public string SearchTerm { get; set; }
    public GameCenterTeamsSessionStatus? SessionStatus { get; set; }

    // page and sort
    public int? PageNumber { get; set; }
    public GetGameCenterTeamsSort? Sort { get; set; }
    public SortDirection? SortDirection { get; set; }
}

public sealed class GameCenterTeamsResults
{
    public required int NamesPendingApproval { get; set; }
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
    public required long RegisteredOn { get; set; }
    public required Score Score { get; set; }
    public required GameCenterTeamsSession Session { get; set; }
    public required int TicketCount { get; set; }
}

public sealed class GameCenterTeamsAdvancement
{
    public required SimpleEntity FromGame { get; set; }
    public required SimpleEntity FromTeam { get; set; }
    public required double Score { get; set; }
}

public enum GameCenterTeamsSessionStatus
{
    Complete,
    NotStarted,
    Playing
}

public sealed class GameCenterTeamsPlayer
{
    public required string Id { get; set; }
    public required string PendingName { get; set; }
    public required string Name { get; set; }
    public required bool IsReady { get; set; }
    public required SimpleSponsor Sponsor { get; set; }
}

public sealed class GameCenterTeamsSession
{
    public required long? Start { get; set; }
    public required long? End { get; set; }
    public required long? TimeCumulativeMs { get; set; }
    public required double? TimeRemainingMs { get; set; }
    public required double? TimeSinceStartMs { get; set; }
}
