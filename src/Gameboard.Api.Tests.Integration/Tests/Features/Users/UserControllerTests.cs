using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration.Users;

public class UserControllerTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public UserControllerTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Fact]
    public async Task Create_WhenDoesntExist_IsCreatedWithId()
    {
        // given 
        _testContext.WithTestServices(s => s.AddGbIntegrationTestAuth(UserRole.Registrar));
        var newUser = new Gameboard.Api.NewUser();

        // when 
        var result = await _testContext
            .Http
            .PostAsync("api/user", newUser.ToJsonBody())
            .WithContentDeserializedAs<Gameboard.Api.User>();

        // then
        result?.Id.ShouldNotBeNullOrEmpty();
    }
}
