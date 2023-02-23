using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.GameEngine;

public interface IGameState
{
    public string Id { get; }
    public string Name { get; }
    public string ManagerId { get; }
    public string ManagerName { get; }
    public string Markdown { get; }
    public string Audience { get; }
    public string LaunchpointUrl { get; }
    public IEnumerable<IGameStatePlayer> Players { get; }
    public DateTimeOffset WhenCreated { get; }
    public DateTimeOffset StartTime { get; }
    public DateTimeOffset EndTime { get; }
    public DateTimeOffset ExpirationTime { get; }
    public bool IsActive { get; }
    public IEnumerable<IGameStateVmState> Vms { get; }
    public IGameStateChallengeView Challenge { get; }
}

public interface IGameStatePlayer
{
    public string GamespaceId { get; }
    public string SubjectId { get; }
    public string SubjectName { get; }
    public IGameStatePlayerPermission Permission { get; }
    public bool IsManager { get; }
}

public enum IGameStatePlayerPermission
{
    None = 0,
    Editor = 1,
    Manager = 2
}

public interface IGameStateVmState
{
    public string Id { get; }
    public string Name { get; }
    public string IsolationId { get; }
    public bool IsRunning { get; }
    public bool IsVisible { get; }
}

public interface IGameStateChallengeView
{
    public string Text { get; }
    public int MaxPoints { get; }
    public int MaxAttempts { get; }
    public int Attempts { get; }
    public double Score { get; }
    public int SectionCount { get; }
    public int SectionIndex { get; }
    public double SectionScore { get; }
    public string SectionText { get; }
    public DateTimeOffset LastScoreTime { get; }
    public IEnumerable<IGameStateQuestionView> Questions { get; }
}

public interface IGameStateQuestionView
{
    public string Answer { get; }
    public string Example { get; }
    public string Hint { get; }
    public bool IsCorrect { get; }
    public bool IsGraded { get; }
    public float Penalty { get; }
    public string Text { get; }
    public float Weight { get; }
}

public class TopoGameState : TopoMojo.Api.Client.GameState, IGameState { }
