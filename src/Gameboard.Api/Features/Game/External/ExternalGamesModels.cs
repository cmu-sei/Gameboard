using System;
using System.Collections.Generic;
using Gameboard.Api.Common;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Features.Games.External;

internal class ExternalSyncGameDeployContext
{
    public required IEnumerable<string> TeamIds { get; set; }
    public required IList<Challenge> DeployedChallenges { get; set; }
    public required IList<GameEngineGameState> DeployedGamespaces { get; set; }
}

public sealed class ExternalSyncGameStartRequest
{
    public required string GameId { get; set; }
}

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
}

public sealed class ExternalGameStartTeamGamespace
{
    public required string Id { get; set; }
    public required SimpleEntity Challenge { get; set; }
    public required IEnumerable<string> VmUrls { get; set; }
}
