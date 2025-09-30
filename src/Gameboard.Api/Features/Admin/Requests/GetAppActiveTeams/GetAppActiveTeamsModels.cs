// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Gameboard.Api.Features.Admin;

public record GetAppActiveTeamsResponse(IEnumerable<AppActiveTeam> Teams);

public sealed class AppActiveTeam
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required DateRange Session { get; set; }
    public required AppActiveTeamGame Game { get; set; }
    public required int DeployedChallengeCount { get; set; }
    public required bool HasTickets { get; set; }
    public required bool IsLateStart { get; set; }
    public required double Score { get; set; }
    public required double? MsRemaining { get; set; }
}

public sealed class AppActiveTeamGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsTeamGame { get; set; }
}
