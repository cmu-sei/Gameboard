// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Data;
using Gameboard.Api.Features.SystemNotifications;

namespace Gameboard.Api.Tests.Integration;

public class SystemNotificationsCreateTests : IClassFixture<GameboardTestContext>
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
                u.Role = UserRoleKey.Admin;
            });
        });

        var title = fixture.Create<string>();

        var notification = new CreateSystemNotification
        {
            Title = title,
            MarkdownContent = fixture.Create<string>(),
            StartsOn = fixture.Create<DateTimeOffset>(),
            IsDismissible = true
        };

        // when we create it 
        var result = await _testContext
            .CreateHttpClientWithActingUser(u =>
            {
                u.Id = userId;
                u.Role = UserRoleKey.Admin;
            })
            .PostAsync("api/system-notifications", notification.ToJsonBody())
            .DeserializeResponseAs<ViewSystemNotification>();

        // then we should get a sensible result
        result.Title.ShouldBe(title);
        result.IsDismissible.ShouldBeTrue();
        result.EndsOn.ShouldBeNull();
    }
}
