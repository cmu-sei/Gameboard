using System;

namespace Gameboard.Api.Features.ChallengeEvents;

public class ChallengeEventSummary
{
    public string UserId { get; set; }
    public string Text { get; set; }
    public ChallengeEventType Type { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class NewChallengeEvent
{
    public string ChallengeId { get; set; }
    public string UserId { get; set; }
    public string TeamId { get; set; }
    public string Text { get; set; }
    public ChallengeEventType Type { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}