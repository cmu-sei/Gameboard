using Gameboard.Api;
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
        var newUser = new Gameboard.Api.NewUser();

        // when 
        var client = _testContext.CreateHttpClientWithAuthRole(UserRole.Registrar);
        var result = await client
            .PostAsync("api/user", newUser.ToJsonBody())
            .WithContentDeserializedAs<Gameboard.Api.User>();

        // then
        result?.Id.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_WhenExists_Throws()
    {
        // given
        await _testContext
            .WithDataState(state =>
            {
                state.AddUser(u =>
                {
                    u.Id = "1234";
                });
            });

        var newUser = new NewUser { Id = "1234" };

        // when 
        var client = this._testContext
            .CreateHttpClientWithAuthRole(UserRole.Registrar);

        var result = await client
            .PostAsync("api/user", newUser.ToJsonBody())
            .WithContentDeserializedAs<Gameboard.Api.User>();

        // then
        // result
    }
}
