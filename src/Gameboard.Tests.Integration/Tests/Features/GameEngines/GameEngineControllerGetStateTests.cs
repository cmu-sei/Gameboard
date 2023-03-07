using System.Text.Json;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Tests.Integration;

public class GameEngineControllerGetStateTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public GameEngineControllerGetStateTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task GameEngineController_WithTopoStateAndEngine_ReturnsCompleteState(IFixture fixture)
    {
        // given 
        var teamId = fixture.Create<string>();
        var playerId = fixture.Create<string>();
        await _testContext.WithDataState(state =>
        {
            state.AddPlayer(p =>
            {
                p.Id = playerId;
                p.TeamId = teamId;
            });

            state.AddChallenge(c =>
            {
                c.Id = fixture.Create<string>();
                c.GameEngineType = Api.GameEngineType.TopoMojo;
                c.PlayerId = playerId;
                // NOTE: this isn't random - it's handcrafted so we can verify the data "tree"
                // See Fixtures/SpecimenBuilders/GameStateBuilder.cs
                c.State = JsonSerializer.Serialize(fixture.Create<TopoMojo.Api.Client.GameState>());
                c.TeamId = teamId;
            });
        });

        var httpClient = _testContext.CreateHttpClientWithAuthRole(Api.UserRole.Admin);

        // when
        var result = await httpClient
            .GetAsync($"/api/gameEngine/state?teamId={teamId}")
            .WithContentDeserializedAs<GameEngineGameState>();

        // then
        result?.Audience.ShouldBe("gameboard");
        result?.Vms.ToArray()[1].IsVisible.ShouldBeFalse();
    }
}
