using System.Text.Json;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class GameEngineControllerGetStateTests
{
    private readonly GameboardTestContext _testContext;

    public GameEngineControllerGetStateTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task GameEngineController_WithTwoTopoStatesAndEngine_ReturnsCompleteStates
    (
        string playerId,
        string teamId,
        string challenge1Id,
        TopoMojo.Api.Client.GameState state1,
        string challenge2Id,
        TopoMojo.Api.Client.GameState state2
    )
    {
        // given 
        await _testContext.WithDataState(state =>
        {
            state.AddPlayer(p =>
            {
                p.Id = playerId;
                p.TeamId = teamId;
            });

            state.AddChallenge(c =>
            {
                c.Id = challenge1Id;
                c.GameEngineType = GameEngineType.TopoMojo;
                c.PlayerId = playerId;
                // NOTE: this isn't random - it's handcrafted so we can verify the data "tree"
                // See Fixtures/SpecimenBuilders/GameStateBuilder.cs
                c.State = JsonSerializer.Serialize(state1);
                c.TeamId = teamId;
            });

            state.AddChallenge(c =>
            {
                c.Id = challenge2Id;
                c.GameEngineType = GameEngineType.TopoMojo;
                c.PlayerId = playerId;
                c.State = JsonSerializer.Serialize(state2);
                c.TeamId = teamId;
            });
        });

        var httpClient = _testContext.CreateHttpClientWithAuthRole(UserRole.Admin);

        // when
        var results = await httpClient
            .GetAsync($"/api/gameEngine/state?teamId={teamId}")
            .WithContentDeserializedAs<IEnumerable<GameEngineGameState>>();

        // then
        results?.Count().ShouldBe(2);
        results?.Select(r => r.Audience).All(a => a == "gameboard").ShouldBeTrue();
        results?.First().Vms.ToArray()[1].IsVisible.ShouldBeFalse();
    }
}
