using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Scores;

public sealed class CantRescoreChallengeWithANonZeroBonus : GameboardValidationException
{
    public CantRescoreChallengeWithANonZeroBonus(string challengeId, string teamId, string bonusId, double pointValue)
        : base($"""Challenge "{challengeId}" (for team "{teamId}") can't be re-scored, because the team has already received bonus "{bonusId}" for {pointValue} points.""") { }
}

public sealed class CantAwardNegativePointValue : GameboardValidationException
{
    public CantAwardNegativePointValue(string challengeId, string teamId, double pointValue) : base($"""Can't award a non-positive point value ({pointValue}) to team "{teamId}" for challenge "{challengeId}".""") { }
}
