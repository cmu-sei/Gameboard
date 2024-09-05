using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class ChallengeBonusControllerManualTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task AddManual_WithChallenge_Succeeds(string challengeId, string userId, string description, double pointsValue, IFixture fixture)
    {
        // given
        var dbContext = await _testContext.GetDbContext();
        var bonuses = await dbContext.ManualBonuses.ToArrayAsync();

        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Challenge>(fixture, c => c.Id = challengeId);
            state.Add<Data.User>(fixture, u =>
            {
                u.Id = userId;
                u.Role = UserRole.Admin;
            });
        });

        var bonus = new CreateManualBonus
        {
            Description = description,
            PointValue = pointsValue,
        };

        // when
        await _testContext
            .CreateHttpClientWithActingUser(u =>
            {
                u.Id = userId;
                u.Role = UserRole.Admin;
            })
            .PostAsync($"api/challenge/{challengeId}/bonus/manual", bonus.ToJsonBody());

        // then
        var storedBonus = await dbContext
            .ManualBonuses
            .AsNoTracking()
            .Where(b => b.Type == ManualBonusType.Challenge)
            .FirstAsync();

        storedBonus.EnteredByUserId.ShouldBe(userId);
        storedBonus.PointValue.ShouldBe(pointsValue);
        storedBonus.EnteredOn.ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }
}
