using Gameboard.Api;
using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration;

public class PlayerControllerTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public PlayerControllerTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Fact]
    public async Task Update_WhenNameNotUniqueInGame_SetsNameNotUnique()
    {
        // given
        await _testContext
            .WithDataState(state =>
            {
                state.AddGame(g =>
                {
                    g.Players = new Api.Data.Player[]
                    {
                        state.BuildPlayer(p =>
                        {
                            p.Id = "PlayerA";
                            p.Name = "A";
                            p.TeamId = "team A";
                        }),

                        state.BuildPlayer(p =>
                        {
                            p.Id = "PlayerB";
                            p.Name = "B";
                            p.TeamId = "team B";
                        })
                    };
                });
            });

        var httpClient = _testContext.CreateHttpClientWithAuth();
        var sutParams = new ChangedPlayer
        {
            Id = "PlayerB",
            // tries to update `playerB` to have the same name as `playerA`
            Name = "A",
            ApprovedName = "B",
            Sponsor = "sponsor",
            Role = PlayerRole.Member
        };

        // when
        var updatedPlayer = await httpClient
            .PutAsync("/api/player", sutParams.ToJsonBody())
            .WithContentDeserializedAs<Api.Player>(_testContext.GetJsonSerializerOptions());

        // assert
        updatedPlayer.NameStatus.ShouldBe(AppConstants.NameStatusNotUnique);
    }
}