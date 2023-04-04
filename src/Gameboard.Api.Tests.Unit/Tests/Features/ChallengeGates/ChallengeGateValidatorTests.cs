using Gameboard.Api.ChallengeGates;

namespace Gameboard.Api.Tests.Unit;

public class ChallengeGateValidatorTests
{
    [Theory, GameboardAutoData]
    public void DetectCycles_WhenSourceAndTargetAreSame_ReturnsSummary(ChallengeGateValidator sut, string challengeId)
    {
        // arrange
        // autofixture power!

        // act
        var result = sut.DetectCycles(challengeId, challengeId);

        // assert
        result.ShouldBe($"{challengeId} => {challengeId}");
    }
}
