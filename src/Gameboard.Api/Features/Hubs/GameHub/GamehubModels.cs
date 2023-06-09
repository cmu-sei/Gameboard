using System.Threading.Tasks;
using Gameboard.Api.Common;

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
    SyncStartGameStarting,
    SyncStartGameStateChanged,
    VerifyAllPlayersConnectedStart,
    VerifyAllPlayersConnectedProgressChange,
    VerifyAllPlayersConnectedEnd,
    YouJoined
}

public interface IGameHubEvent
{
    Task ExternalGameChallengesDeployStart(GameHubEvent<GameStartState> ev);
    Task ExternalGameChallengesDeployProgressChange(GameHubEvent<GameStartState> ev);
    Task ExternalGameChallengesDeployEnd(GameHubEvent<GameStartState> ev);
    Task ExternalGameLaunchStart(GameHubEvent<GameStartState> ev);
    Task ExternalGameLaunchEnd(GameHubEvent<GameStartState> ev);
    Task ExternalGameLaunchFailure(GameHubEvent<GameStartState> ev);
    Task ExternalGameGamespacesDeployStart(GameHubEvent<GameStartState> ev);
    Task ExternalGameGamespacesDeployProgressChange(GameHubEvent<GameStartState> ev);
    Task ExternalGameGamespacesDeployEnd(GameHubEvent<GameStartState> ev);
    Task PlayerJoined(GameHubEvent<PlayerJoinedEvent> ev);
    Task SyncStartGameStateChanged(GameHubEvent<SyncStartState> ev);
    Task SyncStartGameStarting(GameHubEvent<SyncStartGameStartedState> ev);
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
