using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class ScoringControllerTeamGameSummaryTests
{
    private readonly GameboardTestContext _testContext;

    public ScoringControllerTeamGameSummaryTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task GetTeamGameSummary_WithFixedTeamAndChallenges_CalculatesScore
    (
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
            var enteringAdmin = state.Build<Data.User>(fixture, u => u.Role = UserRole.Admin);
            state.Add(enteringAdmin);

            // build the team and give them one challenge
            var builtTeam = state.AddTeam(fixture, t =>
            {
                t.Challenge = new SimpleEntity { Id = challenge1Id, Name = fixture.Create<string>() };
                t.TeamId = teamId;
            });

            // configure points and bonuses for first challenge
            foreach (var challenge in builtTeam.Game.Challenges.Where(c => c.Id == challenge1Id))
            {
                challenge.Points = basePoints1;
                challenge.AwardedManualBonuses = new ManualChallengeBonus[]
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
            }

            // add a second challenge with one bonus
            foreach (var player in builtTeam.Game.Players)
            {
                player.Challenges.Add(new Data.Challenge
                {
                    Id = challenge2Id,
                    TeamId = builtTeam.TeamId,
                    Points = basePoints2,
                    AwardedManualBonuses = new ManualChallengeBonus
                    {
                        Id = fixture.Create<string>(),
                        Description = fixture.Create<string>(),
                        PointValue = bonus3points,
                        EnteredByUserId = enteringAdmin.Id
                    }.ToCollection()
                });
            }
        });

        // anon access is ok 👍
        var httpClient = _testContext.CreateClient();

        // when
        var result = await httpClient
            .GetAsync($"api/team/{teamId}/score")
            .WithContentDeserializedAs<TeamGameScoreSummary>();

        // then
        result.ShouldNotBeNull();
        result.TotalScore.ShouldBe(basePoints1 + basePoints2 + bonus1Points + bonus2Points + bonus3points);
    }
}
