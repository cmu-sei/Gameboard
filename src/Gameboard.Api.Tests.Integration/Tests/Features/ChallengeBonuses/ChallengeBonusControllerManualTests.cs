using Gameboard.Api.Data;
using Gameboard.Api.Features.ChallengeBonuses;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class ChallengeBonusControllerManualTests
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
            // state.AddUser(u =>
            // {

            // });

            // state.AddChallenge(c =>
            // {
            //     c.Id = challengeId;
            // });
        });

        var bonus = new CreateManualChallengeBonus
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
            .ManualChallengeBonuses
            .AsNoTracking()
            .FirstAsync();

        storedBonus.EnteredByUserId.ShouldBe(userId);
        storedBonus.PointValue.ShouldBe(pointsValue);
        storedBonus.EnteredOn.ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }
}
