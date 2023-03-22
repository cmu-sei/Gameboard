using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.GameEngine;

public class GameEngineChallengeRegistration
{
    public required Api.Data.Challenge Challenge { get; set; }
    public required Api.Data.ChallengeSpec ChallengeSpec { get; set; }
    public required Api.Data.Game Game { get; set; }
    public required string GraderKey { get; set; }
    public required string GraderUrl { get; set; }
    public required Api.Data.Player Player { get; set; }
    public required int PlayerCount { get; set; }
    public int Variant { get; set; }
}

public class GameEngineGameState
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ManagerId { get; set; }
    public string ManagerName { get; set; }
    public string Markdown { get; set; }
    public string Audience { get; set; }
    public string LaunchpointUrl { get; set; }
    public bool IsActive { get; set; }

    public IEnumerable<GameEnginePlayer> Players { get; set; }
    public DateTimeOffset WhenCreated { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateTimeOffset ExpirationTime { get; set; }
    public IEnumerable<GameEngineVmState> Vms { get; set; }
    public GameEngineChallengeView Challenge { get; set; }
}

public class GameEnginePlayer
{
    public string GamespaceId { get; set; }
    public string SubjectId { get; set; }
    public string SubjectName { get; set; }
    public GameEnginePlayerPermission Permission { get; set; }
    public bool IsManager { get; set; }
}

public class GameEngineVmState
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string IsolationId { get; set; }
    public bool IsRunning { get; set; }
    public bool IsVisible { get; set; }
}

public class GameEngineChallengeView
{
    public string Text { get; set; }
    public int MaxPoints { get; set; }
    public int MaxAttempts { get; set; }
    public int Attempts { get; set; }
    public double Score { get; set; }
    public int SectionCount { get; set; }
    public int SectionIndex { get; set; }
    public double SectionScore { get; set; }
    public string SectionText { get; set; }
    public DateTimeOffset LastScoreTime { get; set; }
    public IEnumerable<GameEngineQuestionView> Questions { get; set; }
}

public class GameEngineQuestionView
{
    public string Answer { get; set; }
    public string Example { get; }
    public string Hint { get; }
    public bool IsCorrect { get; set; }
    public bool IsGraded { get; set; }
    public float Penalty { get; }
    public string Text { get; set; }
    public float Weight { get; set; }
}

public class GameEngineSectionSubmission
{
    public string Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public int SectionIndex { get; set; }
    public IEnumerable<GameEngineAnswerSubmission> Questions { get; set; }
}

public class GameEngineAnswerSubmission
{
    public string Answer { get; set; }
}

public enum GameEnginePlayerPermission
{
    None = 0,
    Editor = 1,
    Manager = 2
}
