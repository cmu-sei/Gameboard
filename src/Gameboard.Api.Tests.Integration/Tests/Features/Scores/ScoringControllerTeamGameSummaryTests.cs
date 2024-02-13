using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;

namespace Gameboard.Api.Tests.Integration;

public class ScoringControllerTeamGameSummaryTests : IClassFixture<GameboardTestContext>
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
        string gameId,
        string playerId,
        string playerUserId,
        string teamId,
        string challenge1Id,
        string challenge2Id,
        int baseScore1,
        int baseScore2,
        int bonus1Points,
        int bonus2Points,
        int bonus3Points)
    {
        // GIVEN
        await _testContext.WithDataState(state =>
        {
            // user adding the bonus points
            var enteringAdmin = state.Build<Data.User>(fixture, u => u.Role = UserRole.Admin);
            state.Add(enteringAdmin);

            // build the game - 1 player, 2 challenges, 3 bonuses across all
            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.Players = state.Build<Data.Player>(fixture, p =>
                {
                    p.Id = playerId;
                    // need to rethink default entities
                    p.User = state.Build<Data.User>(fixture, u => u.Id = playerUserId);
                    p.TeamId = teamId;
                    p.Challenges = new List<Data.Challenge>
                    {
                        state.Build<Data.Challenge>(fixture, c =>
                        {
                            c.Id = challenge1Id;
                            c.Points = baseScore1;
                            c.Score = baseScore1;
                            c.PlayerId = playerId;
                            c.GameId = gameId;
                            c.TeamId = teamId;
                            c.AwardedManualBonuses = new List<ManualChallengeBonus>()
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
                        }),
                        state.Build<Data.Challenge>(fixture, c =>
                        {
                            c.Id = challenge2Id;
                            c.Points = baseScore2;
                            c.Score = baseScore2;
                            c.PlayerId = playerId;
                            c.GameId = gameId;
                            c.TeamId = teamId;
                            c.AwardedManualBonuses = new ManualChallengeBonus
                            {
                                Id = fixture.Create<string>(),
                                Description = fixture.Create<string>(),
                                PointValue = bonus3Points,
                                EnteredByUserId = enteringAdmin.Id
                            }.ToCollection();
                        })
                    };
                }).ToCollection();
            });
        });

        // only accessible by elevated roles or players on the team 👍
        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = playerUserId);

        // when
        var result = await httpClient
            .GetAsync($"api/team/{teamId}/score")
            .WithContentDeserializedAs<TeamScoreQueryResponse>();

        // then
        result.ShouldNotBeNull();
        result.Score.OverallScore.TotalScore.ShouldBe(baseScore1 + baseScore2 + bonus1Points + bonus2Points + bonus3Points);
    }
}
