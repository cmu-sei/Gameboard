using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Games.External;

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
