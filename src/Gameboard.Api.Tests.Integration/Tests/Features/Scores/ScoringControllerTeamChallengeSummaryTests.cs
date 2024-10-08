using Gameboard.Api.Features.Scores;

namespace Gameboard.Api.Tests.Integration;

public class ScoringControllerTeamChallengeSummaryTests : IClassFixture<GameboardTestContext>
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
        string enteringAdminId,
        string specId,
        string teamId,
        string challengeId,
        int baseScore,
        int bonus1Points,
        int bonus2Points
    )
    {
        // given
        await _testContext.WithDataState(state =>
        {
            var enteringAdmin = state.Build<Data.User>(fixture, u => u.Id = enteringAdminId);
            state.Add(enteringAdmin);

            // have to add spec separately because of broken FK issue
            state.Add<Data.ChallengeSpec>(fixture, s => s.Id = specId);

            state.Add<Data.Challenge>(fixture, c =>
            {
                c.Id = challengeId;
                c.TeamId = teamId;
                c.Player = state.Build<Data.Player>(fixture, p => p.TeamId = teamId);
                c.Points = baseScore;
                c.Score = baseScore;
                c.SpecId = specId;
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

        // when
        var result = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = enteringAdminId)
            .GetAsync($"api/challenge/{challengeId}/score")
            .DeserializeResponseAs<TeamChallengeScore>();

        // then
        result.ShouldNotBeNull();
        result.Score.TotalScore.ShouldBe(baseScore + bonus1Points + bonus2Points);
    }
}
