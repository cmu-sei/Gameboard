using System.Collections.Generic;

namespace Gameboard.Api.Features.Challenges;

public sealed class GetChallengePlayConfigResponse
{
    public required ChallengePlayConfig Config { get; set; }
}

public sealed class ChallengePlayConfig
{
    public required SimpleEntity Challenge { get; set; }
    public required int AttemptsUsed { get; set; }
    public required int? AttemptsMax { get; set; }
    public required bool IsPractice { get; set; }
    public required IEnumerable<ChallengePlayConfigQuestion> Questions { get; set; }
    public required double ScoreMax { get; set; }
    public required double Score { get; set; }
    public required ChallengeSolutionGuide SolutionGuide { get; set; }
    public required long? TimeEnd { get; set; }
    public required long TimeStart { get; set; }
    public required string TeamId { get; set; }
}

public sealed class ChallengePlayConfigQuestion
{
    public required string SampleAnswer { get; set; }
    public required string Text { get; set; }
    public required IEnumerable<string> PreviousResponses { get; set; }
}
