namespace Gameboard.Api.Tests.Integration.Users;

[Collection(TestCollectionNames.DbFixtureTests)]
public class UserControllerTests
{
    private readonly GameboardTestContext _testContext;

    public UserControllerTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task Create_WhenDoesntExist_IsCreatedWithIdAndIsNewUser(string id, IFixture fixture)
    {
        // given 
        await _testContext
            .WithDataState(state =>
            {
                state.Add(fixture.Create<Data.Sponsor>());
            });
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

    [Theory, GbIntegrationAutoData]
    public async Task Create_WhenExists_IsNotNewUser(string userId, IFixture fixture)
    {
        // given
        await _testContext
            .WithDataState(state =>
            {
                state.Add<Data.User>(fixture, u => u.Id = userId);
                state.Add<Data.Sponsor>(fixture);
            });

        var newUser = new NewUser { Id = userId };

        // when 
        var result = await _testContext
            .CreateHttpClientWithAuthRole(UserRole.Registrar)
            .PostAsync("api/user", newUser.ToJsonBody())
            .WithContentDeserializedAs<TryCreateUserResult>();

        // then
        result?.IsNewUser.ShouldBeFalse();
    }
}
