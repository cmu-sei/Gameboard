using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Admin;

public sealed class GameCenterPracticeContext
{
    public required IEnumerable<GameCenterPracticeContextUser> Users { get; set; }
}

public sealed class GameCenterPracticeContextUser
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required SimpleSponsor Sponsor { get; set; }
    public required IEnumerable<GameCenterPracticeContextChallenge> Challenges { get; set; }
}

public sealed class GameCenterPracticeContextChallenge
{
    public required string Id { get; set; }
    public required SimpleEntity ChallengeSpec { get; set; }
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
