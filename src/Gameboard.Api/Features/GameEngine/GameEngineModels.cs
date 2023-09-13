using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

// These are data-annotated to support automatic JSON schema generation for Gamebrain dev
namespace Gameboard.Api.Features.GameEngine;

public class GameEngineChallengeRegistration
{
    public required Data.Challenge Challenge { get; set; }
    public required Data.ChallengeSpec ChallengeSpec { get; set; }
    public required Data.Game Game { get; set; }
    public required string GraderKey { get; set; }
    public required string GraderUrl { get; set; }
    public required Data.Player Player { get; set; }
    public required int PlayerCount { get; set; }
    public required bool StartGamespace { get; set; }
    public int Variant { get; set; }
}

public class GameEngineGamespaceStartRequest
{
    public required string ChallengeId { get; set; }
    public required GameEngineType GameEngineType { get; set; }
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

    public bool HasDeployedGamespace
    {
        get => Vms != null && Vms.Any();
    }
}

public class GameEnginePlayer
{
    [Required] public string GamespaceId { get; set; }
    [Required] public string SubjectId { get; set; }
    [Required] public string SubjectName { get; set; }
    [Required] public GameEnginePlayerPermission Permission { get; set; }
    [Required] public bool IsManager { get; set; }
}

public class GameEngineVmState
{
    [Required] public string Id { get; set; }
    [Required] public string Name { get; set; }
    [Required] public string IsolationId { get; set; }
    [Required] public bool IsRunning { get; set; }
    [Required] public bool IsVisible { get; set; }
}

// this is currently a separate model from `GameEngineVmState` because I don't want to misrepresent
// the state information that Gameboard is getting from Topo. The `GameEngineGamespaceVm` model is
// really here to hold the computed URL of the console for clients.
public class GameEngineGamespaceVm
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
}

public class GameEngineChallengeView
{
    public string Text { get; set; }
    [Required] public double MaxPoints { get; set; }
    [Required] public int MaxAttempts { get; set; }
    [Required] public int Attempts { get; set; }
    [Required] public double Score { get; set; }
    [Required] public int SectionCount { get; set; }
    [Required] public int SectionIndex { get; set; }
    [Required] public double SectionScore { get; set; }
    public string SectionText { get; set; }
    [Required] public DateTimeOffset LastScoreTime { get; set; }
    [Required] public IEnumerable<GameEngineQuestionView> Questions { get; set; }
}

public class GameEngineQuestionView
{
    [Required] public string Answer { get; set; }
    public string Example { get; set; }
    public string Hint { get; set; }
    [Required] public bool IsCorrect { get; set; }
    [Required] public bool IsGraded { get; set; }
    [Required] public float Penalty { get; set; }
    [Required] public string Text { get; set; }
    [Required] public float Weight { get; set; }
}

public class GameEngineSectionSubmission
{
    public required string Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public required int SectionIndex { get; set; }
    public required IEnumerable<GameEngineAnswerSubmission> Questions { get; set; }
}

public class GameEngineAnswerSubmission
{
    public required string Answer { get; set; }
}

public enum GameEnginePlayerPermission
{
    None = 0,
    Editor = 1,
    Manager = 2
}
