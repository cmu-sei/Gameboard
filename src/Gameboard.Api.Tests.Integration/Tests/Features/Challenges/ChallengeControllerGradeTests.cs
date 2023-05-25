using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Tests.Integration;

public class ChallengeControllerGradeTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public ChallengeControllerGradeTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task Grade_WithSingleUnawardedSolveRankBonus_AwardsBonus
    (
        string challengeId,
        string challengeSpecId,
        string bonusId,
        string teamId,
        IFixture fixture
    )
    {
        var baseScore = 100;
        var bonus = 20;

        // given
        await _testContext
            .WithTestServices(services =>
            {
                services.AddGbIntegrationTestAuth(UserRole.Admin);
                services.AddTransient<ITestGradingResultService>(factory =>
                    new TestGradingResultService(() => new GameEngineGameState
                    {
                        Id = challengeId,
                        Challenge = new GameEngineChallengeView
                        {
                            MaxPoints = baseScore,
                            Score = baseScore
                        }
                    }));
            })
            .WithDataState(state =>
            {
                state.AddChallengeSpec(spec =>
                {
                    spec.Id = challengeSpecId;
                    spec.Points = baseScore;
                    spec.Bonuses = new ChallengeBonus[]
                    {
                        new ChallengeBonusCompleteSolveRank
                        {
                            Id = bonusId,
                            PointValue = bonus,
                            SolveRank = 1
                        }
                    };
                });

                state.AddChallenge(c =>
                {
                    c.Id = challengeId;
                    c.SpecId = challengeSpecId;
                    c.TeamId = teamId;
                });
            });


        var httpClient = _testContext.CreateGbApiClient();
        var submission = new GameEngineSectionSubmission
        {
            Id = fixture.Create<string>(),
            Timestamp = DateTimeOffset.Now.AddMinutes(1),
            SectionIndex = 0,
            Answers = new GameEngineAnswerSubmission[]
            {
                new GameEngineAnswerSubmission { Answer = fixture.Create<string>() }
            }
        };

        // when
        var result = await httpClient
            .PutAsync("/api/challenge/grade", submission.ToJsonBody())
            .WithContentDeserializedAs<TeamChallengeScore>();

        // then
        result.ShouldNotBeNull();
        result.Score.CompletionScore.ShouldBe(baseScore);
        result.Score.BonusScore.ShouldBe(bonus);

    }
}
