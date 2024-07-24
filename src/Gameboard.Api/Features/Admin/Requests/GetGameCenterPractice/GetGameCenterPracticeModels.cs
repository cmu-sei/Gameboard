using System.Collections.Generic;

namespace Gameboard.Api.Features.Admin;

public sealed class GameCenterPracticeContext
{
    public required SimpleEntity Game { get; set; }
    public required IEnumerable<GameCenterPracticeContextUser> Users { get; set; }
}

public sealed class GameCenterPracticeContextUser
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required SimpleSponsor Sponsor { get; set; }
    public required SimpleEntity ActiveChallenge { get; set; }
    public required long? ActiveChallengeEndTimestamp { get; set; }
    public required int TotalAttempts { get; set; }
    public required int UniqueChallengeSpecs { get; set; }
    public required IEnumerable<GameCenterPracticeContextChallengeSpec> ChallengeSpecs { get; set; }
}

public sealed class GameCenterPracticeContextChallengeSpec
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Tag { get; set; }
    public required int AttemptCount { get; set; }
    public required long? LastAttemptDate { get; set; }
    public required GameCenterPracticeContextChallengeAttempt BestAttempt { get; set; }
}

public sealed class GameCenterPracticeContextChallengeAttempt
{
    public required long AttemptTimestamp { get; set; }
    public required ChallengeResult Result { get; set; }
    public required double Score { get; set; }
}

public enum GameCenterPracticeSessionStatus
{
    NotPlaying,
    Playing
}

public enum GameCenterPracticeSort
{
    AttemptCount,
    Name,
    Score
}

public record GetGameCenterPracticeContextRequest(GameCenterPracticeSessionStatus? SessionStatus, GameCenterPracticeSort? Sort);
