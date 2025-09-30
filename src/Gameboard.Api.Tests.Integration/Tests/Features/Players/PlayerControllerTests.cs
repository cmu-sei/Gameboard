// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration;

public class PlayerControllerTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task Update_WhenNameNotUniqueInGame_SetsNameNotUnique(IFixture fixture, string playerAId, string playerBId)
    {
        // given
        await _testContext
            .WithDataState(state =>
            {
                state.Add<Data.Game>(fixture, g =>
                {
                    g.Players =
                    [
                        state.Build<Data.Player>(fixture, p =>
                        {
                            p.Id = playerAId;
                            p.Name = "A";
                            p.TeamId = "team A";
                        }),
                        state.Build<Data.Player>(fixture, p =>
                        {
                            p.Id = playerBId;
                            p.Name = "B";
                            p.TeamId = "team B";
                        })
                    ];
                });
            });

        var sutParams = new ChangedPlayer
        {
            Id = playerBId,
            // tries to update `playerB` to have the same name as `playerA`
            Name = "A",
            ApprovedName = "B"
        };

        // when
        var updatedPlayer = await _testContext
            .CreateHttpClientWithAuthRole(UserRoleKey.Admin)
            .PutAsync("/api/player", sutParams.ToJsonBody())
            .DeserializeResponseAs<Player>();

        // assert
        updatedPlayer?.NameStatus.ShouldBe(AppConstants.NameStatusNotUnique);
    }
}
