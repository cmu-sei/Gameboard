using Gameboard.Api.Common;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Tests.Integration;

public class GameControllerGetSyncStartStateTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public GameControllerGetSyncStartStateTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task GetSyncStartState_WithAllReady_IsReady(IFixture fixture)
    {
        // given two players registered for the same sync-start game, both ready
        var gameId = fixture.Create<string>();

        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.RequireSynchronizedStart = true;
                g.Players = new List<Data.Player>
                {
                    state.Build<Data.Player>(fixture, p => p.IsReady = true),
                    state.Build<Data.Player>(fixture, p => p.IsReady = true),
                };
            });
        });

        var http = _testContext.CreateHttpClientWithAuthRole(UserRole.Admin);

        // when
        var result = await http
            .GetAsync($"/api/game/{gameId}/ready")
            .WithContentDeserializedAs<SyncStartState>();

        // then
        result.ShouldNotBeNull();
        result.IsReady.ShouldBeTrue();
        result.Teams.Count().ShouldBe(2);
    }

    [Theory, GbIntegrationAutoData]
    public async Task GetSyncStartState_WithNotReady_IsNotReady(string gameId, string readyPlayerId, string notReadyPlayerId, IFixture fixture)
    {
        // given two players registered for the same sync-start game, one not ready
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.RequireSynchronizedStart = true;
                g.Players = new List<Data.Player>
                {
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = readyPlayerId;
                        p.IsReady = true;
                    }),
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = notReadyPlayerId;
                        p.IsReady = false;
                    }),
                };
            });
        });

        var http = _testContext.CreateHttpClientWithAuthRole(UserRole.Admin);

        // when 
        var result = await http
            .GetAsync($"/api/game/{gameId}/ready")
            .WithContentDeserializedAs<SyncStartState>();

        // then
        result.ShouldNotBeNull();
        result.IsReady.ShouldBeFalse();
        result.Teams.Count().ShouldBe(2);
        result
            .Teams
            .Single(t => !t.IsReady)
            .Players
            .Any(p => p.Id == notReadyPlayerId)
            .ShouldBeTrue();
    }

    [Theory, GbIntegrationAutoData]
    public async Task GetSyncStartState_WithNotRequiredSyncStart_IsReady(IFixture fixture)
    {
        // given a player registered for a game which doesn't require sync start
        var gameId = fixture.Create<string>();
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.RequireSynchronizedStart = false;
                g.Players = state.Build<Data.Player>(fixture, p => p.IsReady = false).ToCollection();
            });
        });

        // when
        await _testContext
            .CreateDefaultClient()
            .GetAsync($"/api/game/{gameId}/ready")
            // then
            .ShouldYieldGameboardValidationException<GameboardAggregatedValidationExceptions>();
    }
}
