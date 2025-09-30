// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.Games;

public class GameHubEvent
{
    public required string GameId { get; set; }
    public required IEnumerable<string> TeamIds { get; set; }
}

public class GameHubEvent<TData> : GameHubEvent where TData : class
{
    public required TData Data { get; set; }
}

public class GameHubLaunchFailureEventData
{
    public required string Message { get; set; }
}

public interface IGameHubEvent
{
    Task ChallengesDeployStart(GameHubEvent<GameResourcesDeployStatus> ev);
    Task ChallengesDeployProgressChange(GameHubEvent<GameResourcesDeployStatus> ev);
    Task ChallengesDeployEnd(GameHubEvent<GameResourcesDeployStatus> ev);
    Task LaunchStart(GameHubEvent ev);
    Task LaunchEnd(GameHubEvent<GameResourcesDeployStatus> ev);
    Task LaunchFailure(GameHubEvent<GameHubLaunchFailureEventData> ev);
    Task LaunchProgressChanged(GameHubEvent<GameResourcesDeployStatus> ev);
    Task GamespacesDeployStart(GameHubEvent<GameResourcesDeployStatus> ev);
    Task GamespacesDeployProgressChange(GameHubEvent<GameResourcesDeployStatus> ev);
    Task GamespacesDeployEnd(GameHubEvent<GameResourcesDeployStatus> ev);
    Task SyncStartGameStateChanged(GameHubEvent<SyncStartState> ev);
    Task SyncStartGameStarted(GameHubEvent<SyncStartGameStartedState> ev);
    Task SyncStartGameStarting(GameHubEvent<SyncStartState> ev);
}
