using System;
using System.Collections.Generic;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Admin;

public sealed class ExternalGameAdminContext
{
    public required SimpleEntity Game { get; set; }
    public required ExternalGameAdminOverallDeployStatus OverallDeployStatus { get; set; }
    public required IEnumerable<SimpleEntity> Specs { get; set; }
    public required DateTimeOffset? StartTime { get; set; }
    public required DateTimeOffset? EndTime { get; set; }
    public required bool HasNonStandardSessionWindow { get; set; }
    public required IEnumerable<ExternalGameAdminTeam> Teams { get; set; }
}

public sealed class ExternalGameAdminTeam
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required ExternalGameTeamDeployStatus DeployStatus { get; set; }
    public required bool IsReady { get; set; }
    public required IEnumerable<SimpleSponsor> Sponsors { get; set; }
    public required IEnumerable<ExternalGameAdminChallenge> Challenges { get; set; }
    public required IEnumerable<ExternalGameAdminPlayer> Players { get; set; }
}

public sealed class ExternalGameAdminChallenge
{
    public required string Id { get; set; }
    public required bool ChallengeCreated { get; set; }
    public required bool GamespaceDeployed { get; set; }
    public required string SpecId { get; set; }
    public required DateTimeOffset? StartTime { get; set; }
    public required DateTimeOffset? EndTime { get; set; }
}

public sealed class ExternalGameAdminPlayer
{
    public string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsCaptain { get; set; }
    public required SimpleSponsor Sponsor { get; set; }
    public required ExternalGameAdminPlayerStatus Status { get; set; }
    public required SimpleEntity User { get; set; }
}

public enum ExternalGameAdminPlayerStatus
{
    NotConnected,
    NotReady,
    Ready
}

public enum ExternalGameAdminOverallDeployStatus
{
    NotStarted,
    PartiallyDeployed,
    Deploying,
    Deployed
}
