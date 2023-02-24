using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.GameEngine;

public interface IGameEngineGameState
{
    string Id { get; }
    string Name { get; set; }
    string ManagerId { get; set; }
    string ManagerName { get; set; }
    string Markdown { get; set; }
    string Audience { get; }
    string LaunchpointUrl { get; }
    IEnumerable<IGameEnginePlayer> Players { get; set; }
    DateTimeOffset WhenCreated { get; set; }
    DateTimeOffset StartTime { get; set; }
    DateTimeOffset EndTime { get; set; }
    DateTimeOffset ExpirationTime { get; set; }
    bool IsActive { get; set; }
    IEnumerable<IGameEngineVmState> Vms { get; set; }
    IGameEngineChallengeView Challenge { get; set; }
}

public interface IGameEnginePlayer
{
    public string GamespaceId { get; }
    public string SubjectId { get; }
    public string SubjectName { get; }
    public GameEnginePlayerPermission Permission { get; }
    public bool IsManager { get; }
}

public interface IGameEngineVmState
{
    string Id { get; set; }
    string Name { get; set; }
    string IsolationId { get; set; }
    bool IsRunning { get; set; }
    bool IsVisible { get; set; }
}

public interface IGameEngineChallengeView
{
    string Text { get; set; }
    int MaxPoints { get; set; }
    int MaxAttempts { get; set; }
    int Attempts { get; set; }
    double Score { get; set; }
    int SectionCount { get; set; }
    int SectionIndex { get; set; }
    double SectionScore { get; set; }
    string SectionText { get; set; }
    DateTimeOffset LastScoreTime { get; set; }
    IEnumerable<IGameEngineQuestionView> Questions { get; set; }
}

public interface IGameEngineQuestionView
{
    string Answer { get; set; }
    string Example { get; }
    string Hint { get; }
    bool IsCorrect { get; set; }
    bool IsGraded { get; set; }
    float Penalty { get; }
    string Text { get; set; }
    float Weight { get; set; }
}

public interface IGameEngineSectionSubmission
{
    string Id { get; }
    DateTimeOffset Timestamp { get; }
    int SectionIndex { get; }
    IEnumerable<IGameEngineAnswerSubmission> Questions { get; }
}

public interface IGameEngineAnswerSubmission
{
    string Answer { get; }
}
