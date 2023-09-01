using System.Net;
using Gameboard.Api.Data;
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
    public async Task GetSyncStartState_WithAllReady_IsReady(string gameId)
    {
        // given two players registered for the same sync-start game, both ready
        await _testContext.WithDataState(state =>
        {
            state.AddGame(g =>
            {
                g.Id = gameId;
                g.RequireSynchronizedStart = true;
            });

            state.AddPlayer(p =>
            {
                p.GameId = gameId;
                p.IsReady = true;
            });

            state.AddPlayer(p =>
            {
                p.GameId = gameId;
                p.IsReady = true;
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
    public async Task GetSyncStartState_WithNotReady_IsNotReady(string gameId, string notReadyPlayerId)
    {
        // given two players registered for the same sync-start game, one not ready
        await _testContext.WithDataState(state =>
        {
            state.AddGame(g =>
            {
                g.Id = gameId;
                g.RequireSynchronizedStart = true;
            });

            state.AddPlayer(p =>
            {
                p.GameId = gameId;
                p.IsReady = true;
            });

            state.AddPlayer(p =>
            {
                p.Id = notReadyPlayerId;
                p.GameId = gameId;
                p.IsReady = false;
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
    public async Task GetSyncStartState_WithNotRequiredSyncStart_IsReady(string gameId)
    {
        // given a player registered for a game which doesn't require sync start
        await _testContext.WithDataState(state =>
        {
            state.AddGame(g =>
            {
                g.Id = gameId;
                g.RequireSynchronizedStart = false;
            });

            state.AddPlayer(p =>
            {
                p.GameId = gameId;
                p.IsReady = false;
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
