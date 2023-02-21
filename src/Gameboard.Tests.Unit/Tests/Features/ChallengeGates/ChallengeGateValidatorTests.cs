using Gameboard.Api;
using Gameboard.Api.ChallengeGates;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;

namespace Gameboard.Tests.Unit;

public class ChallengeGateValidatorTests
{
    [Theory, GameboardAutoData]
    public void DetectCycles_WhenSourceAndTargetAreSame_ReturnsSummary(IFixture fixture)
    {
        // arrange
        var sut = fixture.Create<ChallengeGateValidator>();
        var challengeId = "123";

        // act
        var result = sut.DetectCycles(challengeId, challengeId);

        // assert
        result.ShouldBe($"123 => 123");
    }
}
