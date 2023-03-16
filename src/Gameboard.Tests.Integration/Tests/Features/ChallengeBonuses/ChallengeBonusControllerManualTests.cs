using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Tests.Integration;

public class ChallengeBonusControllerManualTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public ChallengeBonusControllerManualTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task AddManual_WithValidData_Succeeds(string challengeId, string userId, string description, double pointsValue)
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
            });
        });

        var bonus = new CreateManualChallengeBonus
        {
            Description = description,
            PointValue = pointsValue
        };

        var httpClient = _testContext.CreateHttpClientWithActingUser(u =>
        {
            u.Id = userId;
            u.Role = Api.UserRole.Support;
        });

        // when
        await httpClient.PostAsync($"api/challenge/{challengeId}/bonus/manual", bonus.ToJsonBody());

        // then
        var storedBonus = await _testContext
            .GetDbContext()
            .ManualChallengeBonuses
            .AsNoTracking()
            .FirstAsync();

        storedBonus.EnteredByUserId.ShouldBe(userId);
        storedBonus.PointValue.ShouldBe(pointsValue);
        storedBonus.EnteredOn.ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }
}
