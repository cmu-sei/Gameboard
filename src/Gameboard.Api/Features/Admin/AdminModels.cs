using System;
using System.Collections.Generic;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Admin;

public sealed class ExternalGameAdminContext
{
    public SimpleEntity Game { get; set; }
    public IEnumerable<SimpleEntity> Specs { get; set; }
    public bool HasNonStandardSessionWindow { get; set; }
    public IEnumerable<ExternalGameAdminTeam> Teams { get; set; }
}

public sealed class ExternalGameAdminTeam
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ExternalGameDeployStatus DeployStatus { get; set; }
    public IEnumerable<SimpleSponsor> Sponsors { get; set; }
    public IEnumerable<ExternalGameAdminChallenge> Challenges { get; set; }
    public IEnumerable<ExternalGameAdminPlayer> Players { get; set; }
}

public sealed class ExternalGameAdminChallenge
{
    public string Id { get; set; }
    public bool ChallengeCreated { get; set; }
    public bool GamespaceDeployed { get; set; }
    public string SpecId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
}

public sealed class ExternalGameAdminPlayer
{
    public string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsCaptain { get; set; }
    public required SimpleSponsor Sponsor { get; set; }
    public required string Status { get; set; }
    public required SimpleEntity User { get; set; }
}

public static class ExternalGameAdminPlayerStatus
{
    public static string NotConnected = "notConnected";
    public static string NotReady = "notReady";
    public static string Ready = "ready";
}
