// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common;
using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration;

public class PlayerControllerEnrollTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public PlayerControllerEnrollTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task Enroll_WithPriorPracticeModeRegistration_DoesntThrow(string gameId, string userId, IFixture fixture)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Game>(fixture, game =>
            {
                game.Id = gameId;
                game.PlayerMode = PlayerMode.Competition;
            });

            state.Add<Data.User>(fixture, u =>
            {
                u.Id = userId;
                u.Role = UserRoleKey.Member;
                u.Sponsor = fixture.Create<Data.Sponsor>();
                u.Enrollments = state.Build<Data.Player>(fixture, p =>
                {
                    p.Id = fixture.Create<string>();
                    p.Mode = PlayerMode.Practice;
                    p.GameId = gameId;
                }).ToCollection();
            });
        });

        var enrollRequest = new NewPlayer()
        {
            UserId = userId,
            GameId = gameId
        };

        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = userId); ;

        // when
        var result = await httpClient
            .PostAsync("/api/player", enrollRequest.ToJsonBody())
            .DeserializeResponseAs<Player>();

        result.ShouldNotBeNull();
        result.UserId.ShouldBe(userId);
        result.GameId.ShouldBe(gameId);
    }
}
