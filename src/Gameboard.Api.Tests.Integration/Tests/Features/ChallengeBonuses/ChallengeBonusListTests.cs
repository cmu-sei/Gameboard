using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class ChallengeBonusListTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public ChallengeBonusListTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task List_WithTwo_Succeeds(string challengeId, string userId, string bonusOneId, string bonusTwoId, string description, IFixture fixture)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.User>(fixture, u =>
            {
                u.Id = userId;
                u.Role = UserRoleKey.Support;
            });

            state.Add<Data.Challenge>(fixture, c =>
            {
                c.Id = challengeId;
                c.AwardedManualBonuses = new ManualChallengeBonus[]
                {
                    new() {
                        Id = bonusOneId,
                        Description = description,
                        EnteredByUserId = userId,
                        PointValue = 10
                    },
                    new() {
                        Id = bonusTwoId,
                        Description = description,
                        EnteredByUserId = userId,
                        PointValue =  40
                    },
                };
            });


        });

        var http = _testContext.CreateHttpClientWithAuthRole(UserRoleKey.Admin);

        // when
        var bonuses = await http
            .GetAsync($"api/challenge/{challengeId}/bonus/manual")
            .DeserializeResponseAs<IEnumerable<ManualChallengeBonusViewModel>>();

        // then
        bonuses.ShouldNotBeNull();
        bonuses.Count().ShouldBe(2);
        bonuses.Select(b => b.PointValue).Sum().ShouldBe(50);
    }
}
