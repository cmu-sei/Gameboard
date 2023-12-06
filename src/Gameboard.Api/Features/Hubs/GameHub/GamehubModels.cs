using System.Threading.Tasks;

namespace Gameboard.Api.Features.Games;

public class GameHubEvent<TData> where TData : class
{
    public required string GameId { get; set; }
    public required TData Data { get; set; }
    public required GameHubEventType EventType { get; set; }
}

public enum GameHubEventType
{
    ExternalGameChallengesDeployStart,
    ExternalGameChallengesDeployProgressChange,
    ExternalGameChallengesDeployEnd,
    ExternalGameGamespacesDeployStart,
    ExternalGameGamespacesDeployProgressChange,
    ExternalGameGamespacesDeployEnd,
    ExternalGameLaunchStart,
    ExternalGameLaunchEnd,
    ExternalGameLaunchFailure,
    PlayerJoined,
    SyncStartGameStarted,
    SyncStartGameStarting,
    SyncStartGameStateChanged,
    VerifyAllPlayersConnectedStart,
    VerifyAllPlayersConnectedProgressChange,
    VerifyAllPlayersConnectedEnd,
    YouJoined
}

public interface IGameHubEvent
{
    Task ExternalGameChallengesDeployStart(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameChallengesDeployProgressChange(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameChallengesDeployEnd(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameLaunchStart(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameLaunchEnd(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameLaunchFailure(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameGamespacesDeployStart(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameGamespacesDeployProgressChange(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameGamespacesDeployEnd(GameHubEvent<GameStartUpdate> ev);
    Task PlayerJoined(GameHubEvent<PlayerJoinedEvent> ev);
    Task SyncStartGameStateChanged(GameHubEvent<SyncStartState> ev);
    Task SyncStartGameStarted(GameHubEvent<SyncStartGameStartedState> ev);
    Task SyncStartGameStarting(GameHubEvent<SyncStartState> ev);
    Task YouJoined(GameHubEvent<YouJoinedEvent> ev);
}

public class GameJoinRequest
{
    public string GameId { get; set; }
}

public class GameLeaveRequest
{
    public required string GameId { get; set; }
}

public class PlayerJoinedEvent
{
    public required string GameId { get; set; }
    public required SimpleEntity Player { get; set; }
}

public class YouJoinedEvent
{
    public required string GameId { get; set; }
}
