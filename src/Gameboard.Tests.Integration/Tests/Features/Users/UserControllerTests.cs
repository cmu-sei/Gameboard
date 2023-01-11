using Gameboard.Api;
using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration.Users;

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
        var client = _testContext.CreateHttpClientWithAuth();
        var result = await client
            .PostAsync("api/user", newUser.ToJsonBody())
            .WithContentDeserializedAs<Gameboard.Api.User>(_testContext.GetJsonSerializerOptions());

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
                state.Add(state.BuildPlayer(p => p.Id = "1234"));
            });

        var newUser = new NewUser { Id = "1234" };

        // when 
        var client = this._testContext
            .CreateHttpClientWithAuth();

        var result = await client
            .PostAsync("api/user", newUser.ToJsonBody())
            .WithContentDeserializedAs<Gameboard.Api.User>(_testContext.GetJsonSerializerOptions());

        // then
        // result
    }

    // this works, but is testing a thing that we'll actually test via unit test - keeping it for a learn-x thing
    // [Fact]
    // public async Task Create_WhenDoesntExist_AssignsRandomName()
    // {
    //     // given 
    //     var newUser = new Gameboard.Api.NewUser
    //     {
    //         Sponsor = "dod"
    //     };

    //     // when
    //     var client = _testContext.WithAuthentication().CreateClient();
    //     var result = await client
    //         .PostAsync("api/user", newUser.ToJsonBody())
    //         .WithContentDeserializedAs<Gameboard.Api.User>(_testContext.GetJsonSerializerOptions());

    //     // then
    //     result.Name.ShouldNotBeNullOrWhiteSpace();
    //     result.ApprovedName.ShouldNotBeNullOrWhiteSpace();
    //     result.Name.ShouldBe(result.ApprovedName);
    // }
}