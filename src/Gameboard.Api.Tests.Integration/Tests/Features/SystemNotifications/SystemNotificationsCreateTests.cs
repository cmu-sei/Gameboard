using Gameboard.Api.Features.SystemNotifications;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class SystemNotificationsCreateTests
{
    private readonly GameboardTestContext _testContext;

    public SystemNotificationsCreateTests(GameboardTestContext testContext)
        => _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task Create_WithRequiredValues_ReturnsExpected(string userId, IFixture fixture)
    {
        // given an admin creating a new notification (with a few properties to spot check)
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.User>(fixture, u =>
            {
                u.Id = userId;
                u.Role = UserRole.Admin;
            });
        });

        var title = fixture.Create<string>();

        var notification = new CreateSystemNotification
        {
            Title = title,
            MarkdownContent = fixture.Create<string>(),
            StartsOn = fixture.Create<DateTimeOffset>(),
        };

        // when we create it 
        var result = await _testContext
            .CreateHttpClientWithActingUser(u =>
            {
                u.Id = userId;
                u.Role = UserRole.Admin;
            })
            .PostAsync("api/system-notifications", notification.ToJsonBody())
            .WithContentDeserializedAs<ViewSystemNotification>();

        // then we should get a sensible result
        result.Title.ShouldBe(title);
        result.EndsOn.ShouldBeNull();
    }
}
