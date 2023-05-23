namespace Gameboard.Api.Features.Games;

public class GameLauncherChannelEvent<TData> where TData : class
{
    public required string GameId { get; set; }
    public required TData Data { get; set; }
    public required GameLauncherChannelEventType EventType { get; set; }
}

public enum GameLauncherChannelEventType
{
    VerifyAllPlayersConnectedStart,
    VerifyAllPlayersConnectedCountChange,
    VerifyAllPlayersConnectedEnd,
    DeployChallengesStart,
    DeployChallengesPercentageChange,
    DeployChallengesEnd,
    CreatePlayerSessionsStart,
    CreatePlayerSessionPercentageChange,
    CreatePlayerSessionsEnd
}

public interface IGameLauncherChannelBus
{

}
