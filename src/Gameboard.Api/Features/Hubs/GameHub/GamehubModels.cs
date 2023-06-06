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
    DeployChallengesStart,
    DeployChallengesPercentageChange,
    DeployChallengesEnd,
    PlayerJoined,
    SyncStartGameStarting,
    SyncStartGameStateChanged,
    VerifyAllPlayersConnectedStart,
    VerifyAllPlayersConnectedCountChange,
    VerifyAllPlayersConnectedEnd,
}

public interface IGameHubEvent
{
    Task ExternalGameChallengeDeployEvent(GameHubEvent<ExternalGameChallengeCreationState> ev);
    Task PlayerJoined(GameHubEvent<PlayerJoinedEvent> ev);
    Task SyncStartGameStateChanged(GameHubEvent<SyncStartState> ev);
    Task SyncStartGameStarting(GameHubEvent<SyncStartGameStartedState> ev);
}

public class ExternalGameChallengeCreationState
{
    public required string GameId { get; set; }
    public required int ChallengesDeployed { get; set; }
    public required double ChallengesDeployedPercentage { get; set; }
    public required int ChallengesPerPlayer { get; set; }
    public required int ChallengesToDeployRemaining { get; set; }
    public required int ChallengesTotal { get; set; }
    public required int PlayersTotal { get; set; }
    public required TimeSpan TimeElapsed { get; set; }
}

public class GameSessionCreationState
{
    public required string GameId { get; set; }
    public required int TotalPlayers { get; set; }
    public required int PlayersWithSessionCreated { get; set; }
    public required int PlayersWithoutSessionCreated { get; set; }
    public required double PercentageOfSessionsCreated { get; set; }
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
