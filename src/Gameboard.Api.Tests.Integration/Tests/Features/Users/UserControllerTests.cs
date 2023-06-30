using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration.Users;

public class UserControllerTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public UserControllerTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Theory, InlineAutoData]
    public async Task Create_WhenDoesntExist_IsCreatedWithIdAndIsNewUser(string id)
    {
        // given 
        var newUser = new NewUser { Id = id };

        // when 
        var client = _testContext.CreateHttpClientWithAuthRole(UserRole.Registrar);
        var result = await client
            .PostAsync("api/user", newUser.ToJsonBody())
            .WithContentDeserializedAs<TryCreateUserResult>();

        // then
        result?.User.Id.ShouldBe(id);
        result?.IsNewUser.ShouldBeTrue();
    }

    // [Theory, InlineAutoData]
    // public async Task Create_WhenDoesntExist_IsCreatedWithIdAndIsNewUser(string id)
    // {
    //     // given 
    //     var newUser = new NewUser { Id = id };

    //     // when 
    //     var client = _testContext.CreateHttpClientWithAuthRole(UserRole.Registrar);
    //     var result = await client
    //         .PostAsync("api/user", newUser.ToJsonBody())
    //         .WithContentDeserializedAs<TryCreateUserResult>();

    //     // then
    //     result?.User.Id.ShouldBe(id);
    //     result?.IsNewUser.ShouldBeTrue();
    // }

    [Fact]
    public async Task Create_WhenExists_IsNotNewUser()
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
            .WithContentDeserializedAs<TryCreateUserResult>();

        // then
        result?.IsNewUser.ShouldBeFalse();
    }
}
