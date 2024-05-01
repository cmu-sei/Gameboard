
using System.Collections.Generic;
using System.Linq;
using Gameboard.Api;
using Gameboard.Api.Features.GameEngine;
using MediatR;

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

public sealed record GameResourcesDeployStartNotification(IEnumerable<string> TeamIds) : INotification;

public sealed record GameResourcesDeployEndNotification(IEnumerable<string> TeamIds) : INotification;
