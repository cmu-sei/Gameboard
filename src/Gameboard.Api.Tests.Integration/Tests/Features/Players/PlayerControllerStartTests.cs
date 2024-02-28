using System.Net;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Player;

namespace Gameboard.Api.Tests.Integration;

public class PlayerControllerStartTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public PlayerControllerStartTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task Start_WithClosedExecutionWindow_Throws
    (
        IFixture fixture,
        string playerId,
        string sponsorId,
        string userId
    )
    {
        // given a game with a closed execution window and a registered player
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Sponsor>(fixture, s => s.Id = sponsorId);
            state.Add<Data.Game>(fixture, g =>
            {
                g.GameStart = DateTime.UtcNow.AddDays(-2);
                g.GameEnd = DateTime.UtcNow.AddDays(-1);

                g.Players = new Data.Player[]
                {
                    new()
                    {
                        Id = playerId,
                        SponsorId = sponsorId,
                        User = new Data.User { Id = userId, SponsorId = sponsorId }
                    }
                };
            });
        });

        // when the player tries to start their session
        var startRequest = new SessionStartRequest { PlayerId = playerId };
        await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PutAsync($"/api/player/{playerId}/start", startRequest.ToJsonBody())
            // we expect a validation exception
            .ShouldYieldGameboardValidationException<GameNotActive>();
    }

    [Theory, GbIntegrationAutoData]
    public async Task Start_WithAlmostClosedExecutionWindowAndLateStartDisabled_Throws
    (
        IFixture fixture,
        string playerId,
        string sponsorId,
        string userId
    )
    {
        // given a game with a closed execution window and a registered player
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Sponsor>(fixture, s => s.Id = sponsorId);
            state.Add<Data.Game>(fixture, g =>
            {
                g.GameStart = DateTime.UtcNow.AddDays(-2);
                g.GameEnd = DateTime.UtcNow.AddHours(1);
                g.SessionMinutes = 120;
                g.AllowLateStart = false;

                g.Players = new Data.Player[]
                {
                    new()
                    {
                        Id = playerId,
                        SponsorId = sponsorId,
                        TeamId = fixture.Create<string>(),
                        User = new Data.User { Id = userId, SponsorId = sponsorId }
                    }
                };
            });
        });

        // when the player tries to start their session
        var startRequest = new SessionStartRequest { PlayerId = playerId };
        await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PutAsync($"/api/player/{playerId}/start", startRequest.ToJsonBody())
            .ShouldYieldGameboardValidationException<CantLateStart>();
    }

    [Theory, GbIntegrationAutoData]
    public async Task Start_WithAlmostClosedExecutionWindowAndLateStartEnabled_AllowsShortSession
    (
        IFixture fixture,
        string playerId,
        string sponsorId,
        string userId
    )
    {
        // given a game with a closed execution window and a registered player
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Sponsor>(fixture, s => s.Id = sponsorId);
            state.Add<Data.Game>(fixture, g =>
            {
                g.GameStart = DateTime.UtcNow.AddDays(-2);
                g.GameEnd = DateTime.UtcNow.AddHours(1);
                g.SessionMinutes = 120;
                g.AllowLateStart = true;

                g.Players = new Data.Player[]
                {
                    new()
                    {
                        Id = playerId,
                        SponsorId = sponsorId,
                        TeamId = fixture.Create<string>(),
                        User = new Data.User { Id = userId, SponsorId = sponsorId }
                    }
                };
            });
        });

        // when the player tries to start their session
        var startRequest = new SessionStartRequest { PlayerId = playerId };
        var result = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PutAsync($"/api/player/{playerId}/start", startRequest.ToJsonBody())
            .WithContentDeserializedAs<Player>();

        // the player should have a shortened session window consistent with the remaining execution time
        Math.Round(result.SessionMinutes).ShouldBe(60);
    }
}
