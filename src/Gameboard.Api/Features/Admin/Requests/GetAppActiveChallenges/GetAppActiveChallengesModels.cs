using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Admin;

public sealed class AppActiveChallengeSpec
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Tag { get; set; }
    public required AppActiveChallengeGame Game { get; set; }
    public required IEnumerable<AppActiveChallenge> Challenges { get; set; }
}

public sealed class AppActiveChallenge
{
    public required string Id { get; set; }
    public required AppActiveChallengeTeam Team { get; set; }
    public required DateTimeOffset StartedAt { get; set; }
    public required bool HasTickets { get; set; }
}

public sealed class AppActiveChallengeGame
{
    public required string Id { get; set; }
    public required GameEngineType Engine { get; set; }
    public required string Name { get; set; }
    public required bool IsTeamGame { get; set; }
}

public sealed class AppActiveChallengeTeam
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required DateRange Session { get; set; }
}
