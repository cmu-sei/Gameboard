using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Games.External;

public sealed class GetExternalGameHostClientInfo
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string ClientUrl { get; set; }
}

public sealed class GetExternalGameHostsResponseHost
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string ClientUrl { get; set; }
    public required bool DestroyResourcesOnDeployFailure { get; set; }
    public required int? GamespaceDeployBatchSize { get; set; }
    public required string HostApiKey { get; set; }
    public required string HostUrl { get; set; }
    public required string PingEndpoint { get; set; }
    public required string StartupEndpoint { get; set; }
    public required string TeamExtendedEndpoint { get; set; }
    public required IEnumerable<SimpleEntity> UsedByGames { get; set; }
}

public sealed class ExternalGameState
{
    public required SimpleEntity Game { get; set; }
    public required ExternalGameDeployStatus OverallDeployStatus { get; set; }
    public required IEnumerable<SimpleEntity> Specs { get; set; }
    public required DateTimeOffset? StartTime { get; set; }
    public required DateTimeOffset? EndTime { get; set; }
    public required bool HasNonStandardSessionWindow { get; set; }
    public required bool IsSyncStart { get; set; }
    public required IEnumerable<ExternalGameStateTeam> Teams { get; set; }
}

public sealed class ExternalGameStateTeam
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required ExternalGameDeployStatus DeployStatus { get; set; }
    public required string ExternalGameHostUrl { get; set; }
    public required bool IsReady { get; set; }
    public required IEnumerable<SimpleSponsor> Sponsors { get; set; }
    public required IEnumerable<ExternalGameStateChallenge> Challenges { get; set; }
    public required IEnumerable<ExternalGameStatePlayer> Players { get; set; }
}

public sealed class ExternalGameStateChallenge
{
    public required string Id { get; set; }
    public required bool ChallengeCreated { get; set; }
    public required bool GamespaceDeployed { get; set; }
    public required string SpecId { get; set; }
    public required DateTimeOffset? StartTime { get; set; }
    public required DateTimeOffset? EndTime { get; set; }
}

public sealed class ExternalGameStatePlayer
{
    public string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsCaptain { get; set; }
    public required SimpleSponsor Sponsor { get; set; }
    public required ExternalGameStatePlayerStatus Status { get; set; }
    public required SimpleEntity User { get; set; }
}

public enum ExternalGameStatePlayerStatus
{
    NotConnected,
    NotReady,
    Ready
}

public enum ExternalGameDeployStatus
{
    NotStarted,
    PartiallyDeployed,
    Deploying,
    Deployed
}

/// <summary>
/// This is basically a "frozen" API that pushes data to an external
/// game host after the game is launched. 
/// 
/// CHANGING THIS WILL REQUIRE EXTERNAL HOSTS (E.G. GAMEBRAIN) TO
/// CHANGE THEIR CODE.
/// </summary>
public sealed class ExternalGameStartMetaData
{
    public required SimpleEntity Game { get; set; }
    public required ExternalGameStartMetaDataSession Session { get; set; }
    public required IEnumerable<ExternalGameStartMetaDataTeam> Teams { get; set; }
}

public sealed class ExternalGameStartMetaDataSession
{
    public required DateTimeOffset Now { get; set; }
    public required DateTimeOffset SessionBegin { get; set; }
    public required DateTimeOffset SessionEnd { get; set; }
}

public sealed class ExternalGameStartMetaDataTeam
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required IEnumerable<ExternalGameStartTeamGamespace> Gamespaces { get; set; }
    public required IEnumerable<ExternalGameStartMetaDataPlayer> Players { get; set; }
}

public sealed class ExternalGameStartTeamGamespace
{
    public required string Id { get; set; }
    public required IEnumerable<string> VmUris { get; set; }
    public required bool IsDeployed { get; set; }
}

public sealed class ExternalGameStartMetaDataPlayer
{
    public required string PlayerId { get; set; }
    public required string UserId { get; set; }
}

public sealed class ExternalGameClientTeamConfig
{
    public required string TeamID { get; set; }
    public required string HeadlessServerUrl { get; set; }
}

public sealed class ExternalGameDeployTeamResourcesRequest
{
    public IEnumerable<string> TeamIds { get; set; }
}
