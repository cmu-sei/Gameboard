// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

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
