namespace Gameboard.Api.Data;

public static class ChallengeExtensions
{
    public static double GetPercentMaxPointsScored(this Challenge challenge)
    {
        return (double)(challenge.Points != 0 ? decimal.Divide(new decimal(challenge.Score), challenge.Points) : 0);
    }

    public static ChallengeResult GetResult(this Challenge challenge)
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
