namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class UserControllerCreateTests
{
    private readonly GameboardTestContext _testContext;

    public UserControllerCreateTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public Task UserControllerCreate_WithClaims_Creates(IFixture fixture)
    {
        return Task.CompletedTask;
    }
}
