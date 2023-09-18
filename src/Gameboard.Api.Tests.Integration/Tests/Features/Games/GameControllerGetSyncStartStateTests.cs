using System.Net;
using Gameboard.Api.Common;
using Gameboard.Api.Features.Games;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class GameControllerGetSyncStartStateTests
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
            state.AddGame(g =>
            {
                g.Id = gameId;
                g.RequireSynchronizedStart = true;
                g.Players = new Data.Player[]
                {
                    state.BuildPlayer(fixture, p => p.IsReady = true),
                    state.BuildPlayer(fixture, p => p.IsReady = true),
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
            state.AddGame(g =>
            {
                g.Id = gameId;
                g.RequireSynchronizedStart = true;
                g.Players = new Data.Player[]
                {
                    state.BuildPlayer(fixture, p =>
                    {
                        p.Id = readyPlayerId;
                        p.IsReady = true;
                    }),
                    state.BuildPlayer(fixture, p =>
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
            .Where(t => !t.IsReady)
            .Single()
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
            state.AddGame(g =>
            {
                g.Id = gameId;
                g.RequireSynchronizedStart = false;
                g.Players = state.BuildPlayer(fixture, p =>
                {
                    p.IsReady = false;
                }).ToCollection();
            });
        });

        var client = _testContext.CreateHttpClientWithAuthRole(UserRole.Admin);
        var response = await client.GetAsync($"/api/game/{gameId}/ready");
        var isGbValidationException = await response.Content.IsGameboardValidationException();

        // when / then
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        isGbValidationException.ShouldBeTrue();
    }
}
