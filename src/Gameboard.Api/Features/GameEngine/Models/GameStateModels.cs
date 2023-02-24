using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.GameEngine;

public class GameEngineGameState : IGameEngineGameState
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ManagerId { get; set; }
    public string ManagerName { get; set; }
    public string Markdown { get; set; }
    public string Audience { get; }
    public string LaunchpointUrl { get; }
    public bool IsActive { get; set; }

    public IEnumerable<IGameEnginePlayer> Players { get; set; }
    public DateTimeOffset WhenCreated { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateTimeOffset ExpirationTime { get; set; }
    public IEnumerable<IGameEngineVmState> Vms { get; set; }
    public IGameEngineChallengeView Challenge { get; set; }
}

public class GameEnginePlayer : IGameEnginePlayer
{
    public string GamespaceId { get; set; }
    public string SubjectId { get; set; }
    public string SubjectName { get; set; }
    public GameEnginePlayerPermission Permission { get; set; }
    public bool IsManager { get; set; }
}

public class GameEngineVmState : IGameEngineVmState
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string IsolationId { get; set; }
    public bool IsRunning { get; set; }
    public bool IsVisible { get; set; }
}

public class GameEngineChallengeView : IGameEngineChallengeView
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
    public IEnumerable<IGameEngineQuestionView> Questions { get; set; }
}

public class GameEngineQuestionView : IGameEngineQuestionView
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

public class GameEngineSectionSubmission : IGameEngineSectionSubmission
{
    public string Id { get; }
    public DateTimeOffset Timestamp { get; }
    public int SectionIndex { get; }
    public IEnumerable<IGameEngineAnswerSubmission> Questions { get; }
}

public class GameEngineAnswerSubmission : IGameEngineAnswerSubmission
{
    public string Answer { get; set; }
}

public enum GameEnginePlayerPermission
{
    None = 0,
    Editor = 1,
    Manager = 2
}
