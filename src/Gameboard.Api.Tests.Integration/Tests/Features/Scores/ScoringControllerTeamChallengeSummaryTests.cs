using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class ScoringControllerTeamChallengeSummaryTests
{
    private readonly GameboardTestContext _testContext;

    public ScoringControllerTeamChallengeSummaryTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task GetChallengeScore_WithFixedTeam_CalculatesExpectedScore
    (
        IFixture fixture,
        string teamId,
        string challengeId,
        int basePoints,
        int bonus1Points,
        int bonus2Points
    )
    {
        // given
        await _testContext.WithDataState(state =>
        {
            var enteringAdmin = state.Build<Data.User>(fixture);
            state.Add(enteringAdmin);

            state.Add<Data.Challenge>(fixture, c =>
            {
                c.Id = challengeId;
                c.TeamId = teamId;
                c.Points = basePoints;
                c.AwardedManualBonuses = new List<Data.ManualChallengeBonus>
                {
                    new()
                    {
                        Id = fixture.Create<string>(),
                        Description = fixture.Create<string>(),
                        PointValue = bonus1Points,
                        EnteredByUserId = enteringAdmin.Id
                    },
                    new()
                    {
                        Id = fixture.Create<string>(),
                        Description = fixture.Create<string>(),
                        PointValue = bonus2Points,
                        EnteredByUserId = enteringAdmin.Id
                    }
                };
            });
        });

        var httpClient = _testContext.CreateClient();

        // when
        var result = await httpClient
            .GetAsync($"api/challenge/{challengeId}/score")
            .WithContentDeserializedAs<TeamChallengeScoreSummary>();

        // then
        result.ShouldNotBeNull();
        result.TotalScore.ShouldBe(basePoints + bonus1Points + bonus2Points);
    }
}
