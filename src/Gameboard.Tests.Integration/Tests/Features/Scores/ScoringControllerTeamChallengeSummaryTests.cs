using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration;

public class ScoringControllerTeamChallengeSummaryTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public ScoringControllerTeamChallengeSummaryTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task GetChallengeScore_WithFixedTeam_CalculatesScore(IFixture fixture, string teamId, string challengeId, int basePoints, int bonus1Points, int bonus2Points)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            var builtTeam = state.AddTeam(fixture, t =>
            {
                t.ChallengeId = challengeId;
                t.TeamId = teamId;
            });

            var enteringAdmin = state.BuildUser();
            state.Add(enteringAdmin);

            builtTeam.Challenge.Points = basePoints;
            builtTeam.Challenge.AwardedManualBonuses = new ManualChallengeBonus[]
            {
                new ManualChallengeBonus
                {
                    Id = fixture.Create<string>(),
                    Description = fixture.Create<String>(),
                    PointValue = bonus1Points,
                    EnteredByUserId = enteringAdmin.Id
                },
                new ManualChallengeBonus
                {
                    Id = fixture.Create<string>(),
                    Description = fixture.Create<string>(),
                    PointValue = bonus2Points,
                    EnteredByUserId = enteringAdmin.Id
                }
            };
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
