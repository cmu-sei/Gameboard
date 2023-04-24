using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class ChallengeBonusListTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public ChallengeBonusListTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task List_WithTwo_Succeeds(string challengeId, string userId, string bonusOneId, string bonusTwoId, string description)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            state.AddUser(u =>
            {
                u.Id = userId;
                u.Role = Api.UserRole.Support;
            });

            state.AddChallenge(c =>
            {
                c.Id = challengeId;
                c.AwardedManualBonuses = new ManualChallengeBonus[]
                {
                    new ManualChallengeBonus
                    {
                        Id = bonusOneId,
                        Description = description,
                        EnteredByUserId = userId,
                        PointValue = 10
                    },
                    new ManualChallengeBonus
                    {
                        Id = bonusTwoId,
                        Description = description,
                        EnteredByUserId = userId,
                        PointValue = 40
                    },
                };
            });


        });

        var httpClient = _testContext.CreateHttpClientWithActingUser(u =>
        {
            u.Id = userId;
            u.Role = Api.UserRole.Support;
        });

        // when
        var bonuses = await httpClient.GetAsync($"api/challenge/{challengeId}/bonus/manual")
            .WithContentDeserializedAs<IEnumerable<ManualChallengeBonusViewModel>>();

        // then
        bonuses.ShouldNotBeNull();
        bonuses.Count().ShouldBe(2);
        bonuses.Select(b => b.PointValue).Sum().ShouldBe(50);
    }
}
