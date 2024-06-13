using System;
using System.Collections.Generic;
using System.Linq;

namespace Gameboard.Api.Features.Teams;

public class PromoteToManagerRequest
{
    public User Actor { get; set; }
    public bool AsAdmin { get; set; }
    public string CurrentManagerPlayerId { get; set; }
    public string NewManagerPlayerId { get; set; }
    public string TeamId { get; set; }
}

public sealed class ExtendTeamSessionRequest
{
    public required string TeamId { get; set; }
    public required DateTimeOffset NewSessionEnd { get; set; }
    public required User Actor { get; set; }
}

public class ResetTeamSessionRequest
{
    public required bool UnenrollTeam { get; set; }
}

public sealed class TeamChallengeSolveCounts
{
    public required int Complete { get; set; }
    public required int Partial { get; set; }
    public required int Unscored { get; set; }
}

public class TeamInvitation
{
    public string Code { get; set; }
}

public class TeamAdvancement
{
    public string[] TeamIds { get; set; }
    public string GameId { get; set; }
    public bool WithScores { get; set; }
    public string NextGameId { get; set; }
}

public class ObserveTeam
{
    public required string ApprovedName { get; set; }
    public required string GameId { get; set; }
    public required string TeamId { get; set; }
    public required DateTimeOffset SessionBegin { get; set; }
    public required DateTimeOffset SessionEnd { get; set; }
    public required bool Advanced { get; set; }
    public required int CorrectCount { get; set; }
    public required int PartialCount { get; set; }
    public required int Rank { get; set; }
    public required double Score { get; set; }
    public required long Time { get; set; }
    public required IEnumerable<ObserveTeamPlayer> Members { get; set; }
    public required IEnumerable<Sponsor> Sponsors { get; set; }
}

public sealed class ObserveTeamPlayer
{
    public required string Id { get; set; }
    public required string ApprovedName { get; set; }
    public required PlayerRole Role { get; set; }
    public required string UserId { get; set; }
}

public class Team
{
    public string TeamId { get; set; }
    public string ApprovedName { get; set; }
    public string GameId { get; set; }
    public DateTimeOffset SessionBegin { get; set; }
    public DateTimeOffset SessionEnd { get; set; }
    public int Rank { get; set; }
    public int Score { get; set; }
    public long Time { get; set; }
    public int CorrectCount { get; set; }
    public int PartialCount { get; set; }
    public bool Advanced { get; set; }
    public required SimpleEntity AdvancedFromGame { get; set; }
    public required bool IsAdvancedFromTeamGame { get; set; }
    public required SimpleEntity AdvancedFromPlayer { get; set; }
    public required string AdvancedFromTeamId { get; set; }
    public required double? AdvancedWithScore { get; set; }
    public IEnumerable<TeamChallenge> Challenges { get; set; } = new List<TeamChallenge>();
    public IEnumerable<TeamMember> Members { get; set; } = new List<TeamMember>();
    public IEnumerable<Sponsor> Sponsors { get; set; } = new List<Sponsor>();
}

public class TeamSummary
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Sponsor { get; set; }
    public string TeamSponsors { get; set; }
    public string[] Members { get; set; }
    public string[] SponsorList
    {
        get
        {
            return (TeamSponsors ?? Sponsor ?? string.Empty)
                .Split("|")
                .Where(s => s != string.Empty)
                .ToArray();
        }
    }
}

public class TeamPlayer
{
    public string Id { get; set; }
    public string TeamId { get; set; }
    public string Name { get; set; }
    public string ApprovedName { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string UserApprovedName { get; set; }
    public string UserNameStatus { get; set; }
    public Sponsor Sponsor { get; set; }
    public PlayerRole Role { get; set; }
    public bool IsManager => Role == PlayerRole.Manager;
}

public class NotificationHubTeamSessionEvent
{
    public required string TeamId { get; set; }
    public required DateTimeOffset SessionBegin { get; set; }
    public required DateTimeOffset SessionEnd { get; set; }
}

public class TeamState
{
    public required string Id { get; set; }
    public required string ApprovedName { get; set; }
    public required string Name { get; set; }
    public required string NameStatus { get; set; }
    public required string GameId { get; set; }
    public DateTimeOffset? SessionBegin { get; set; }
    public DateTimeOffset? SessionEnd { get; set; }
    public required SimpleEntity Actor { get; set; }
}

public class TeamHubSessionResetEvent
{
    public required string Id { get; set; }
    public required string GameId { get; set; }
    public required SimpleEntity ActingUser { get; set; }
}
