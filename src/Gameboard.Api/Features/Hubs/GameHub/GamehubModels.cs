using System.Collections.Generic;
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
    ChallengesDeployStart,
    ExternalGameChallengesDeployProgressChange,
    ExternalGameChallengesDeployEnd,
    ExternalGameGamespacesDeployStart,
    ExternalGameGamespacesDeployProgressChange,
    ExternalGameGamespacesDeployEnd,
    ExternalGameLaunchStart,
    ExternalGameLaunchEnd,
    ExternalGameLaunchFailure,
    SyncStartGameStarted,
    SyncStartGameStarting,
    SyncStartGameStateChanged,
    VerifyAllPlayersConnectedStart,
    VerifyAllPlayersConnectedProgressChange,
    VerifyAllPlayersConnectedEnd,
    YourActiveGamesChanged
}

public interface IGameHubEvent
{
    Task ChallengesDeployStart(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameChallengesDeployProgressChange(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameChallengesDeployEnd(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameLaunchStart(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameLaunchEnd(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameLaunchFailure(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameGamespacesDeployStart(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameGamespacesDeployProgressChange(GameHubEvent<GameStartUpdate> ev);
    Task ExternalGameGamespacesDeployEnd(GameHubEvent<GameStartUpdate> ev);
    Task SyncStartGameStateChanged(GameHubEvent<SyncStartState> ev);
    Task SyncStartGameStarted(GameHubEvent<SyncStartGameStartedState> ev);
    Task SyncStartGameStarting(GameHubEvent<SyncStartState> ev);
    Task YourActiveGamesChanged(GameHubEvent<YourActiveGamesChangedEvent> ev);
}

public class GameJoinRequest
{
    public string GameId { get; set; }
}

public class GameLeaveRequest
{
    public required string GameId { get; set; }
}

public sealed class GameHubActiveEnrollment
{
    public required SimpleEntity Game { get; set; }
    public required SimpleEntity Player { get; set; }
}

public sealed class YourActiveGamesChangedEvent
{
    public required string UserId { get; set; }
    public required IEnumerable<GameHubActiveEnrollment> ActiveEnrollments { get; set; }
}
