using Gameboard.Api.Common;
using Gameboard.Api.Features.GameEngine;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class ChallengeControllerGradeTests
{
    private readonly GameboardTestContext _testContext;

    public ChallengeControllerGradeTests(GameboardTestContext testContext)
        => _testContext = testContext;

    // NOTE: This version is trying to use grader key auth, but our test infrastructure
    // isn't correctly allowing us to replace services at test time. need to come back to this.
    [Theory, GbIntegrationAutoData]
    public async Task UpdateTeamChallengeScore_WithFirstSolve_SetsExpectedRankAndScore
    (
        string challengeId,
        string challengeSpecId,
        string graderKey,
        string teamId,
        IFixture fixture
    )
    {
        // given a team with a standard challenge spec scoring for the first time
        // note: we still have to mock the grading stuff because we're not testing with a real engine
        await _testContext
            .WithDataState(state =>
            {
                state.Add<Data.Game>(fixture, g =>
                {
                    g.Specs = state.Build<Data.ChallengeSpec>(fixture, spec =>
                    {
                        spec.Id = challengeSpecId;
                        spec.Points = 100;
                    }).ToCollection();

                    g.Challenges = state.Build<Data.Challenge>(fixture, c =>
                    {
                        c.Id = challengeId;
                        c.GraderKey = graderKey.ToSha256();
                        c.Points = 0;
                        c.Score = 0;
                        c.SpecId = challengeSpecId;
                        c.TeamId = teamId;
                        c.Player = state.Build<Data.Player>(fixture, p =>
                        {
                            p.Score = 0;
                            p.Rank = 0;
                            p.TeamId = teamId;
                        });
                    }).ToCollection();
                });
            });

        // when they score
        await _testContext
            .CreateHttpClientWithGraderConfig(graderKey, 100)
            .PutAsync("/api/challenge/grade", new GameEngineSectionSubmission
            {
                Id = challengeId,
                Timestamp = DateTimeOffset.UtcNow,
                SectionIndex = 0,
                Questions = new GameEngineAnswerSubmission { Answer = "test" }.ToEnumerable()
            }.ToJsonBody())
            .WithContentDeserializedAs<Challenge>();

        // then the players table should have the expected properties set
        var player = await _testContext
            .GetDbContext()
            .Players
            .AsNoTracking()
            .Where(p => p.TeamId == teamId)
            .SingleAsync();

        player.Rank.ShouldBe(1);
        player.Score.ShouldBe(100);
    }
}
