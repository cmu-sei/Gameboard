
using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Features.GameEngine;
using MediatR;

namespace Gameboard.Api.Features.Games;

public sealed class GameResourcesDeployRequest
{
    public required string GameId { get; set; }
    public required IEnumerable<string> SpecIds { get; set; }
    public required IEnumerable<string> TeamIds { get; set; }
}

public sealed class GameResourcesDeployChallenge
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required GameEngineType Engine { get; set; }
    public required bool HasGamespace { get; set; } = false;
    public required bool IsActive { get; set; }
    public required bool IsFullySolved { get; set; }
    public required string SpecId { get; set; }
    public required GameEngineGameState State { get; set; }
    public required string TeamId { get; set; }
}

public sealed class GameResourcesDeployGamespace
{
    public required string Id { get; set; }
    public required IEnumerable<string> VmUris { get; set; }
    public required bool IsDeployed { get; set; }
}

public sealed class GameResourcesDeployGamespacesResult
{
    public required IDictionary<string, GameResourcesDeployGamespace> Gamespaces { get; set; }
    public required IEnumerable<string> FailedGamespaceDeployIds { get; set; }
}

public sealed class GameResourcesDeployResults
{
    public required SimpleEntity Game { get; set; }
    public required IDictionary<string, IEnumerable<GameResourcesDeployChallenge>> TeamChallenges { get; set; }
    public required IEnumerable<string> DeployFailedGamespaceIds { get; set; }

    public IEnumerable<string> GetTeamIds()
        => TeamChallenges.Keys.ToArray();
}

public sealed class GameResourcesDeployStatus
{
    public required SimpleEntity Game { get; set; }
    public required IEnumerable<SimpleEntity> ChallengeSpecs { get; set; } = new List<SimpleEntity>();
    public required IEnumerable<GameResourcesDeployChallenge> Challenges { get; set; } = new List<GameResourcesDeployChallenge>();
    public required IEnumerable<GameResourcesDeployTeam> Teams { get; set; } = new List<GameResourcesDeployTeam>();
    public required IEnumerable<string> FailedGamespaceDeployChallengeIds { get; set; } = new List<string>();
}

public sealed class GameResourcesDeployCaptain
{
    public required SimpleEntity Player { get; set; }
    public required string UserId { get; set; }
}

public sealed class GameResourcesDeployPlayer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string UserId { get; set; }
}

public sealed class GameResourcesDeployTeam
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required SimpleEntity Team { get; set; }
    public required GameResourcesDeployCaptain Captain { get; set; }
    public required IEnumerable<GameResourcesDeployPlayer> Players { get; set; } = new List<GameResourcesDeployPlayer>();
}


public sealed record GameResourcesDeployStartNotification(IEnumerable<string> TeamIds) : INotification;
public sealed record GameResourcesDeployEndNotification(IEnumerable<string> TeamIds) : INotification;

public sealed record ChallengeDeployedNotification(GameResourcesDeployChallenge Challenge) : INotification;
public sealed record ChallengeGamespaceDeployedNotification(string ChallengeId) : INotification;

