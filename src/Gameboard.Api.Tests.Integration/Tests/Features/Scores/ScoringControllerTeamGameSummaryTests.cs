using Gameboard.Api.Data;
using Gameboard.Api.Tests.Shared;

namespace Gameboard.Api.Tests.Integration;

public class ScoringControllerTeamGameSummaryTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public ScoringControllerTeamGameSummaryTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task GetTeamGameSummary_WithFixedTeamAndChallenges_CalculatesScore(
        IFixture fixture,
        string teamId,
        string challenge1Id,
        string challenge2Id,
        int basePoints1,
        int basePoints2,
        int bonus1Points,
        int bonus2Points,
        int bonus3points)
    {
        // GIVEN
        await _testContext.WithDataState(state =>
        {
            // user adding the bonus points
            var enteringAdmin = state.BuildUser();
            state.Add(enteringAdmin);

            // build the team and give them one challenge
            var builtTeam = state.AddTeam(fixture, t =>
            {
                t.Challenge = new SimpleEntity { Id = challenge1Id, Name = fixture.Create<string>() };
                t.TeamId = teamId;
            });

            // configure points and bonuses for first challenge
            builtTeam.Challenge!.Points = basePoints1;
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

            // add a second challenge with one bonus
            state.AddChallenge(c =>
            {
                c.Id = challenge2Id;
                c.TeamId = teamId;
                c.PlayerId = builtTeam.Challenge.Player.Id;
                c.Points = basePoints2;
                c.Game = builtTeam.Game;
                c.AwardedManualBonuses = new ManualChallengeBonus[]
                {
                    new ManualChallengeBonus
                    {
                        Id = fixture.Create<string>(),
                        Description = fixture.Create<string>(),
                        PointValue = bonus3points,
                        EnteredByUserId = enteringAdmin.Id
                    }
                };
            });
        });

        // anon access is ok 👍
        var httpClient = _testContext.CreateClient();

        // when
        var result = await httpClient
            .GetAsync($"api/team/{teamId}/score")
            .WithContentDeserializedAs<TeamGameScoreSummary>();

        // then
        result.ShouldNotBeNull();
        result.Score.TotalScore.ShouldBe(basePoints1 + basePoints2 + bonus1Points + bonus2Points + bonus3points);
    }
}
