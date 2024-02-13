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
    public async Task AddManual_WithValidData_Succeeds(string challengeId, string userId, string description, double pointsValue, IFixture fixture)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.User>(fixture, u =>
            {
                u.Id = userId;
                u.Role = UserRole.Support;
            });

            state.Add<Data.Challenge>(fixture, c => c.Id = challengeId);
        });

        var bonus = new CreateManualBonus
        {
            Description = description,
            PointValue = pointsValue
        };

        var httpClient = _testContext.CreateHttpClientWithActingUser(u =>
        {
            u.Id = userId;
            u.Role = UserRole.Support;
        });

        // when
        await httpClient.PostAsync($"api/challenge/{challengeId}/bonus/manual", bonus.ToJsonBody());

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
