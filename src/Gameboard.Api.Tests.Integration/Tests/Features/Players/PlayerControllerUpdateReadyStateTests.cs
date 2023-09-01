using System.Net;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration.Players;

[Collection(TestCollectionNames.DbFixtureTests)]
public class PlayerControllerUpdatePlayerReadyTests
{
    private readonly GameboardTestContext _testContext;

    public PlayerControllerUpdatePlayerReadyTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    /// <summary>
    /// If a player readies up but the game is not ready to lauch, the API should return 200 + no content.
    /// </summary>
    /// <param name="fixture"></param>
    /// <param name="gameId"></param>
    /// <param name="readyPlayerId"></param>
    /// <param name="readyPlayerUserId"></param>
    /// <param name="notReadyPlayerId"></param>
    /// <returns></returns>
    [Theory, GbIntegrationAutoData]
    public async Task UpdatePlayerReady_WithNonReadyPlayers_DoesNotStartSession(IFixture fixture, string gameId, string notReadyPlayer1Id, string readyPlayer1UserId, string notReadyPlayer2Id)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            state.AddGame(g =>
            {
                g.Id = gameId;
                g.Name = fixture.Create<string>();
                g.RequireSynchronizedStart = true;
                g.Players = new Data.Player[]
                {
                    new Data.Player
                    {
                        Id = notReadyPlayer1Id,
                        Name = "not ready (but will be)",
                        IsReady = false,
                        TeamId = fixture.Create<string>(),
                        User = new Data.User { Id = readyPlayer1UserId }
                    },
                    new Data.Player
                    {
                        Id = notReadyPlayer2Id,
                        Name = "not ready",
                        IsReady = false,
                        TeamId = fixture.Create<string>()
                    }
                };
            });
        });

        var client = _testContext.CreateHttpClientWithActingUser(u => u.Id = readyPlayer1UserId);

        // when
        var response = await client.PutAsync($"/api/player/{notReadyPlayer1Id}/ready", new PlayerReadyUpdate { IsReady = true }.ToJsonBody());

        // then
        // only way to validate is to check for an upcoming session for the game

        var finalPlayer1 = await _testContext
            .GetDbContext()
            .Players
            .SingleOrDefaultAsync(p => p.Id == notReadyPlayer1Id);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        finalPlayer1.ShouldNotBeNull();
        finalPlayer1.SessionBegin.ShouldBe(DateTimeOffset.MinValue);
    }

    [Theory, GbIntegrationAutoData]
    public async Task UpdatePlayerReady_WithAllReadyPlayers_ReturnsStartedSession(IFixture fixture, string gameId, string readyPlayerId, string notReadyPlayerUserId, string notReadyPlayerId)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            state.AddGame(g =>
            {
                g.Id = gameId;
                g.Name = fixture.Create<string>();
                g.RequireSynchronizedStart = true;
                g.Players = new Data.Player[]
                {
                    new Data.Player
                    {
                        Id = notReadyPlayerId,
                        Name = "not ready (but will be)",
                        Role = PlayerRole.Manager,
                        IsReady = false,
                        TeamId = fixture.Create<string>(),
                        User = new Data.User { Id = notReadyPlayerUserId }
                    },
                    new Data.Player
                    {
                        Id = readyPlayerId,
                        Name = "ready",
                        IsReady = true,
                        Role = PlayerRole.Manager,
                        TeamId = fixture.Create<string>()
                    }
                };
            });
        });

        var client = _testContext.CreateHttpClientWithActingUser(u => u.Id = notReadyPlayerUserId);

        // when
        var response = await client.PutAsync($"/api/player/{notReadyPlayerId}/ready", new PlayerReadyUpdate { IsReady = true }.ToJsonBody());

        // then
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.ShouldNotBeNull();

        var gameSyncStartState = await client
            .GetAsync($"/api/game/{gameId}/ready")
            .WithContentDeserializedAs<SyncStartState>();

        gameSyncStartState.ShouldNotBeNull();
        gameSyncStartState.IsReady.ShouldBeTrue();
    }
}
