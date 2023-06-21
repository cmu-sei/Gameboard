namespace Gameboard.Api.Features.Challenges;

public static class ChallengeExtensions
{
    public static ChallengeResult GetResult(this Challenge challenge)
        => GetResult(challenge.Score, challenge.Points);

    public static ChallengeResult GetResult(this Data.Challenge challenge)
        => GetResult(challenge.Score, challenge.Points);

    public static ChallengeResult GetResult(double? score, double possiblePoints)
    {
        if (score == null || score == 0)
            return ChallengeResult.None;
        if (score >= possiblePoints)
            return ChallengeResult.Success;
        if (score > 0)
            return ChallengeResult.Partial;

        return ChallengeResult.None;
    }
}
