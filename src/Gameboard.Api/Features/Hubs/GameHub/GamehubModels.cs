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

public enum GameHubEventType
{
    ChallengesDeployStart,
    ChallengesDeployProgressChange,
    ChallengesDeployEnd,
    GamespacesDeployStart,
    GamespacesDeployProgressChange,
    GamespacesDeployEnd,
    LaunchStart,
    LaunchEnd,
    LaunchFailure,
    SyncStartGameStarted,
    SyncStartGameStarting,
    SyncStartGameStateChanged
}

public interface IGameHubEvent
{
    Task ChallengesDeployStart(GameHubEvent<GameResourcesDeployStatus> ev);
    Task ChallengesDeployProgressChange(GameHubEvent<GameResourcesDeployStatus> ev);
    Task ChallengesDeployEnd(GameHubEvent<GameResourcesDeployStatus> ev);
    Task LaunchStart(GameHubEvent<GameResourcesDeployStatus> ev);
    Task LaunchEnd(GameHubEvent<GameResourcesDeployStatus> ev);
    Task LaunchFailure(GameHubEvent<GameResourcesDeployStatus> ev);
    Task GamespacesDeployStart(GameHubEvent<GameResourcesDeployStatus> ev);
    Task GamespacesDeployProgressChange(GameHubEvent<GameResourcesDeployStatus> ev);
    Task GamespacesDeployEnd(GameHubEvent<GameResourcesDeployStatus> ev);
    Task SyncStartGameStateChanged(GameHubEvent<SyncStartState> ev);
    Task SyncStartGameStarted(GameHubEvent<SyncStartGameStartedState> ev);
    Task SyncStartGameStarting(GameHubEvent<SyncStartState> ev);
}
