using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class ChallengeBonusControllerManualTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public ChallengeBonusControllerManualTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task AddManual_WithChallenge_Succeeds(string challengeId, string userId, string description, double pointsValue, IFixture fixture)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Challenge>(fixture, c => c.Id = challengeId);
            state.Add<Data.User>(fixture, u =>
            {
                u.Id = userId;
                u.Role = UserRole.Support;
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
        var storedBonus = await _testContext
            .GetDbContext()
            .ManualBonuses
            .AsNoTracking()
            .Where(b => b.Type == Data.ManualBonusType.Challenge)
            .FirstAsync();

        storedBonus.EnteredByUserId.ShouldBe(userId);
        storedBonus.PointValue.ShouldBe(pointsValue);
        storedBonus.EnteredOn.ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }
}
