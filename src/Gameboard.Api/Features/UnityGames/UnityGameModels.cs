using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.UnityGames;

public class NewUnityChallenge
{
    public string GameId { get; set; }
    public string PlayerId { get; set; }
    public string TeamId { get; set; }
    public int MaxPoints { get; set; }
    public string GamespaceId { get; set; }
    public IEnumerable<UnityGameVm> Vms { get; set; }
}

public class UnityGameVm
{
    public string Id { get; set; }
    public string Url { get; set; }
    public string Name { get; set; }
}

public class NewUnityChallengeEvent
{
    public string ChallengeId { get; set; }
    public string TeamId { get; set; }
    public string Text { get; set; }
    public ChallengeEventType Type { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}