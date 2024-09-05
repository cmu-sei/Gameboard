using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Scores;

namespace Gameboard.Api.Tests.Integration;

public class ScoringControllerGetGameScoreTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public ScoringControllerGetGameScoreTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task GetGameScore_WithTwoTeamsWithAllBonusesAwarded_ShowsNoUnclaimedBonuses
    (
        string gameId,
        string bonusId,
        string bonusReceivingTeamId,
        string nonBonusReceivingTeamId,
        string bonusReceivingChallengeId,
        string nonBonusReceivingChallengeId,
        string challengeSpecId,
        IFixture fixture
    )
    {
        // given a game with one challenge which awards one bonus to the team who finishes first
        // and two teams, one of which has received the bonus
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.Players = new Data.Player[]
                {
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.TeamId = bonusReceivingTeamId;
                    }),

                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.TeamId = nonBonusReceivingTeamId;
                    })
                };

                g.Challenges = new Data.Challenge[]
                {
                    new()
                    {
                        Id = bonusReceivingChallengeId,
                        Points = 200,
                        TeamId = bonusReceivingTeamId,
                        SpecId = challengeSpecId,
                        AwardedBonuses = new AwardedChallengeBonus
                        {
                            Id = fixture.Create<string>(),
                            ChallengeBonusId = bonusId
                        }.ToCollection()
                    },
                    new ()
                    {
                        Id = nonBonusReceivingChallengeId,
                        Points = 0,
                        SpecId = challengeSpecId,
                        TeamId = nonBonusReceivingTeamId
                    }
                };

                g.Specs = new Data.ChallengeSpec
                {
                    Id = challengeSpecId,
                    GameId = gameId,
                    Bonuses = (new ChallengeBonusCompleteSolveRank
                    {
                        Id = bonusId,
                        PointValue = 100,
                        SolveRank = 1
                    } as ChallengeBonus).ToCollection()
                }.ToCollection();
            });
        });

        var result = await _testContext
            .CreateHttpClientWithAuthRole(UserRole.Admin)
            .GetAsync($"api/game/{gameId}/score")
            .DeserializeResponseAs<GameScore>();

        // the teams should be aware of the game's one challenge:
        result.Teams.All(t => t.Challenges.Any()).ShouldBeTrue();
        result.Teams.Single(t => t.Team.Id == bonusReceivingTeamId).Challenges.Any(c => c.UnclaimedBonuses.Any()).ShouldBeFalse();
        result.Teams.Single(t => t.Team.Id == nonBonusReceivingTeamId).Challenges.Any(c => c.UnclaimedBonuses.Any()).ShouldBeFalse();
    }
}
