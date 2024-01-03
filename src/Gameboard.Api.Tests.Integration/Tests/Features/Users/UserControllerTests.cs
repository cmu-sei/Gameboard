namespace Gameboard.Api.Tests.Integration.Users;

public class UserControllerTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public UserControllerTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task Create_WhenDoesntExist_IsCreatedWithIdAndIsNewUser(string userId, IFixture fixture)
    {
        // given 
        await _testContext
            .WithDataState(state =>
            {
                state.Add(fixture.Create<Data.Sponsor>());
            });
        var newUser = new NewUser { Id = userId };

        // when 
        var result = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PostAsync("api/user", newUser.ToJsonBody())
            .WithContentDeserializedAs<TryCreateUserResult>();

        // then
        result?.User.Id.ShouldBe(userId);
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
