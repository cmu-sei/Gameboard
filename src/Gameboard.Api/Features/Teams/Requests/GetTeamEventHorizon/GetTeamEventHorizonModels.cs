using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Teams;

public enum EventHorizonEventType
{
    ChallengeDeployed,
    GamespaceStarted,
    GamespaceStopped,
    SolveComplete,
    SubmissionRejected,
    SubmissionScored
}

public interface IEventHorizonEvent
{
    public string Id { get; set; }
    public string ChallengeId { get; set; }
    public EventHorizonEventType Type { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class EventHorizonEvent : IEventHorizonEvent
{
    public required string Id { get; set; }
    public required string ChallengeId { get; set; }
    public required EventHorizonEventType Type { get; set; }
    public required DateTimeOffset Timestamp { get; set; }
}

public sealed class EventHorizonSolveCompleteEventData
{
    public required int AttemptsUsed { get; set; }
    public required double FinalScore { get; set; }
}

public sealed class EventHorizonSolveCompleteEvent : EventHorizonEvent
{
    public required EventHorizonSolveCompleteEventData EventData { get; set; }
}

public sealed class EventHorizonSubmissionScoredEventData
{
    public required IEnumerable<string> Answers { get; set; }
    public required int AttemptNumber { get; set; }
    public required double Score { get; set; }
}

public sealed class EventHorizonSubmissionScoredEvent : EventHorizonEvent
{
    public required EventHorizonSubmissionScoredEventData EventData { get; set; }
}

public sealed class EventHorizonGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required IEnumerable<EventHorizonChallengeSpec> ChallengeSpecs { get; set; }
}

public sealed class EventHorizonTeam
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required EventHorizonSession Session { get; set; }
    public required IEnumerable<EventHorizonTeamChallenge> Challenges { get; set; }
    public required IEnumerable<IEventHorizonEvent> Events { get; set; }

}

public sealed class EventHorizonSession
{
    public required DateTimeOffset Begin { get; set; }
    public required DateTimeOffset? End { get; set; }
}

public sealed class EventHorizonChallengeSpec
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required int MaxAttempts { get; set; }
    public required double MaxPossibleScore { get; set; }
}

public sealed class EventHorizonTeamChallenge
{
    public required string Id { get; set; }
    public required string SpecId { get; set; }
}

public sealed class EventHorizon
{
    public required EventHorizonGame Game { get; set; }
    public required EventHorizonTeam Team { get; set; }
}
