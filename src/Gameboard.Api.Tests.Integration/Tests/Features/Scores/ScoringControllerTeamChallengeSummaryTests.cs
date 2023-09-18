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
    public async Task GetChallengeScore_WithFixedTeam_CalculatesScore
    (
        IFixture fixture,
        string teamId,
        string challengeId,
        int basePoints,
        int bonus1Points,
        int bonus2Points)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            var builtTeam = state.AddTeam(fixture, t =>
            {
                t.Challenge = new SimpleEntity { Id = challengeId, Name = fixture.Create<string>() };
                t.TeamId = teamId;
            });

            var enteringAdmin = state.BuildUser();
            state.Add(enteringAdmin);

            builtTeam.Challenge!.Points = basePoints;
            builtTeam.Challenge!.AwardedManualBonuses = new ManualChallengeBonus[]
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
