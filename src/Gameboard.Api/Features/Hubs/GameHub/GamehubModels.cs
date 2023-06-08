using System;
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
    Task ExternalGameChallengesDeployStart(GameHubEvent<ExternalGameLaunchState> ev);
    Task ExternalGameChallengesDeployProgressChange(GameHubEvent<ExternalGameLaunchState> ev);
    Task ExternalGameChallengesDeployEnd(GameHubEvent<ExternalGameLaunchState> ev);
    Task ExternalGameLaunchStart(GameHubEvent<ExternalGameLaunchState> ev);
    Task ExternalGameLaunchEnd(GameHubEvent<ExternalGameLaunchState> ev);
    Task ExternalGameLaunchFailure(GameHubEvent<ExternalGameLaunchState> ev);
    Task ExternalGameGamespacesDeployStart(GameHubEvent<ExternalGameLaunchState> ev);
    Task ExternalGameGamespacesDeployProgressChange(GameHubEvent<ExternalGameLaunchState> ev);
    Task ExternalGameGamespacesDeployEnd(GameHubEvent<ExternalGameLaunchState> ev);
    Task PlayerJoined(GameHubEvent<PlayerJoinedEvent> ev);
    Task SyncStartGameStateChanged(GameHubEvent<SyncStartState> ev);
    Task SyncStartGameStarting(GameHubEvent<SyncStartGameStartedState> ev);
    Task YouJoined(GameHubEvent<YouJoinedEvent> ev);
}

public class ExternalGameLaunchState
{
    public SimpleEntity Game { get; set; }
    public int ChallengesCreated { get; set; } = 0;
    public int ChallengesTotal { get; set; } = 0;
    public int GamespacesDeployed { get; set; } = 0;
    public int GamespacesTotal { get; set; } = 0;
    public int PlayersTotal { get; set; }
    public int TeamsTotal { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset Now { get; set; }
    public string Error { get; set; }

    public double OverallProgress { get => Math.Round(((0.8 * GamespacesDeployed) / GamespacesTotal) + ((0.2 * ChallengesCreated) / ChallengesTotal)); }
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
    public required string ConnectionId { get; set; }
    public required string UserId { get; set; }
    public required int UserCount { get; set; }
    public required string GroupName { get; set; }
}
