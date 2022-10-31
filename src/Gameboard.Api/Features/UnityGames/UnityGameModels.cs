using System;

namespace Gameboard.Api.Features.UnityGames;

public class NewUnityChallenge
{
    public string GameId { get; set; }
    public string TeamId { get; set; }
    public int Points { get; set; }
}

public class NewUnityChallengeEvent
{
    public string ChallengeId { get; set; }
    public string TeamId { get; set; }
    public string Text { get; set; }
    public ChallengeEventType Type { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}