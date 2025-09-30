// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration.Users;

public class UserControllerTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

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
            .DeserializeResponseAs<TryCreateUserResult>();

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
            .CreateHttpClientWithAuthRole(UserRoleKey.Admin)
            .PostAsync("api/user", newUser.ToJsonBody())
            .DeserializeResponseAs<TryCreateUserResult>();

        // then
        result?.IsNewUser.ShouldBeFalse();
    }
}
