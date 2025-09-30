// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Tests.Unit;

public class CumulativeTimeCalculatorTests
{
    [Theory, GameboardAutoData]
    public void CalculateCumulativeTimeMs_WithOneChallenge_EqualsSolveTime
    (
        int scoreTimeOffset,
        IFixture fixture
    )
    {
        // given a challenge with fixed start and end
        var startTime = fixture.Create<DateTimeOffset>();
        var teamTime = new TeamChallengeTime
        {
            TeamId = fixture.Create<string>(),
            ChallengeId = fixture.Create<string>(),
            StartTime = startTime,
            LastScoreTime = startTime.AddMilliseconds(scoreTimeOffset)
        };

        // when it's calculated
        var sut = new CumulativeTimeCalculator();
        var result = sut.CalculativeCumulativeTimeMs([teamTime]);

        // then
        result.ShouldBe(scoreTimeOffset);
    }

    [Theory, GameboardAutoData]
    public void CalculateCumulativeTimeMs_WithTwoChallenge_EqualsSolveTime
    (
        int scoreTimeOffset1,
        int scoreTimeOffset2,
        DateTimeOffset startTime1,
        DateTimeOffset startTime2,
        IFixture fixture
    )
    {
        // given a challenge with fixed start and end
        var teamTimes = new TeamChallengeTime[]
        {
            new()
            {
                TeamId = fixture.Create<string>(),
                ChallengeId = fixture.Create<string>(),
                StartTime = startTime1,
                LastScoreTime = startTime1.AddMilliseconds(scoreTimeOffset1)
            },
            new()
            {
                TeamId = fixture.Create<string>(),
                ChallengeId = fixture.Create<string>(),
                StartTime = startTime2,
                LastScoreTime = startTime2.AddMilliseconds(scoreTimeOffset2)
            }
        };

        // when it's calculated
        var sut = new CumulativeTimeCalculator();
        var result = sut.CalculativeCumulativeTimeMs(teamTimes);

        // then
        result.ShouldBe(scoreTimeOffset1 + scoreTimeOffset2);
    }

    [Theory, GameboardAutoData]
    public void CalculateCumulativeTimeMs_WithUnstartedChallenge_YieldsZero(IFixture fixture)
    {
        // given a challenge with fixed start and end
        var teamTimes = new TeamChallengeTime[]
        {
            new()
            {
                TeamId = fixture.Create<string>(),
                ChallengeId = fixture.Create<string>(),
                StartTime = null,
                LastScoreTime = fixture.Create<DateTimeOffset>()
            }
        };

        // when it's calculated
        var sut = new CumulativeTimeCalculator();
        var result = sut.CalculativeCumulativeTimeMs(teamTimes);

        // then
        result.ShouldBe(0);
    }

    [Theory, GameboardAutoData]
    public void CalculateCumulativeTimeMs_WithUnscoredChallenge_YieldsZero(IFixture fixture)
    {
        // given a challenge with fixed start and end
        var teamTimes = new TeamChallengeTime[]
        {
            new()
            {
                TeamId = fixture.Create<string>(),
                ChallengeId = fixture.Create<string>(),
                StartTime = fixture.Create<DateTimeOffset>(),
                LastScoreTime = null
            }
        };

        // when it's calculated
        var sut = new CumulativeTimeCalculator();
        var result = sut.CalculativeCumulativeTimeMs(teamTimes);

        // then
        result.ShouldBe(0);
    }
}
