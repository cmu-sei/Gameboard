using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Tests.Integration.Teams;

public class TeamControllerStartTeamSessionTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task TeamGame_WithSinglePlayer_CantStart
    (
        string gameId,
        string playerId,
        string userId,
        IFixture fixture
    )
    {
        // given a team game and a registered player with no teammates
        await _testContext.WithDataState(state =>
        {
            state.Add(new Data.Game
            {
                Id = gameId,
                MinTeamSize = 2,
                MaxTeamSize = 5,
                GameStart = DateTimeOffset.UtcNow,
                GameEnd = DateTimeOffset.UtcNow.AddDays(1),
                Mode = GameEngineMode.Standard,
                Players = state.Build<Data.Player>(fixture, p =>
                {
                    p.Id = playerId;
                    p.User = state.Build<Data.User>(fixture, u => u.Id = userId);
                }).ToCollection()
            });
        });

        // when they try to start their session
        await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PutAsync($"api/player/{playerId}/start", null)
            // they should get a validation error
            .ShouldYieldGameboardValidationException<GameboardAggregatedValidationExceptions>();
    }

    [Theory, GbIntegrationAutoData]
    public async Task TeamGame_WithTwoPlayers_CanStart
    (
        string gameId,
        string playerId,
        string userId,
        string teamId,
        IFixture fixture
    )
    {
        // given a team game and a registered player with no teammates
        await _testContext.WithDataState(state =>
        {
            state.Add(new Data.Game
            {
                Id = gameId,
                MinTeamSize = 2,
                MaxTeamSize = 5,
                GameStart = DateTimeOffset.UtcNow,
                GameEnd = DateTimeOffset.UtcNow.AddDays(1),
                Mode = GameEngineMode.Standard,
                Players =
                [
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = playerId;
                        p.Role = PlayerRole.Manager;
                        p.TeamId = teamId;
                        p.User = state.Build<Data.User>(fixture, u => u.Id = userId);
                    }),
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = fixture.Create<string>();
                        p.Role = PlayerRole.Member;
                        p.TeamId = teamId;
                        p.User = state.Build<Data.User>(fixture, u => u.Id = fixture.Create<string>());
                    })
                ]
            });
        });

        // when they try to start their session
        var result = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PutAsync($"api/player/{playerId}/start", null)
            .DeserializeResponseAs<Player>();

        // then we should get a player back with a nonempty session start
        result.SessionBegin.ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }

    [Theory, GbIntegrationAutoData]
    public async Task TeamGame_WithCaptainPromotion_CanStart
    (
        string finalCaptainPlayerId,
        string finalCaptainUserId,
        string initialCaptainPlayerId,
        string initialCaptainUserId,
        string teamId,
        IFixture fixture
    )
    {
        // given a team game and a registered player with no teammates
        await _testContext.WithDataState(state =>
        {
            state.Add(new Data.Game
            {
                Id = fixture.Create<string>(),
                MinTeamSize = 2,
                MaxTeamSize = 5,
                GameStart = DateTimeOffset.UtcNow,
                GameEnd = DateTimeOffset.UtcNow.AddDays(1),
                Mode = GameEngineMode.Standard,
                Players =
                [
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = initialCaptainPlayerId;
                        p.Role = PlayerRole.Manager;
                        p.TeamId = teamId;
                        p.User = state.Build<Data.User>(fixture, u => u.Id = initialCaptainUserId);
                    }),
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = finalCaptainPlayerId;
                        p.Role = PlayerRole.Member;
                        p.TeamId = teamId;
                        p.User = state.Build<Data.User>(fixture, u => u.Id = finalCaptainUserId);
                    })
                ]
            });
        });

        // when they promote a new captain and then start
        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = initialCaptainUserId);

        await httpClient
            .PutAsync($"api/team/{teamId}/manager/{finalCaptainPlayerId}", new PromoteToManagerRequest
            {
                CurrentCaptainId = initialCaptainPlayerId,
                NewManagerPlayerId = finalCaptainPlayerId,
                TeamId = teamId
            }.ToJsonBody());

        var result = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = finalCaptainUserId)
            .PutAsync($"api/player/{finalCaptainPlayerId}/start", null)
            .DeserializeResponseAs<Player>();

        // then we should get a player back with a nonempty session start
        result.SessionBegin.ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }

    // Non-admins can't start sessions for other teams
    [Theory, GbIntegrationAutoData]
    public async Task Team_WhenStartingOtherTeamSession_FailsValidation
    (
        string actingTeamId,
        string actingUserId,
        string targetPlayerId,
        IFixture fixture
    )
    {
        // given two players registered for the same game
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Game>(fixture, game =>
            {
                game.Players =
                [
                    // the person who's starting a session
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = fixture.Create<string>();
                        p.Role = PlayerRole.Manager;
                        p.TeamId = actingTeamId;
                        p.User = state.Build<Data.User>(fixture, u => u.Id = actingUserId);
                    }),
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = targetPlayerId;
                        p.Role = PlayerRole.Manager;
                        p.TeamId = fixture.Create<string>();
                        p.User = state.Build<Data.User>(fixture);
                    })
                ];
            });
        });

        // when the first player tries to start the second's session
        var response = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = actingUserId)
            .PutAsync($"api/player/{targetPlayerId}/start", null);

        // then the response should have a failure code
        response.IsSuccessStatusCode.ShouldBeFalse();
    }

    // Users can team up, leave the team, join a different team, then start sessions on the original and the new team
    // Admins can start sessions for non-admins
    [Theory, GbIntegrationAutoData]
    public async Task Team_WhenAdminStartingOtherTeamSession_Starts
    (
        string targetPlayerId,
        IFixture fixture
    )
    {
        // given two players registered for the same game
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Game>(fixture, game =>
            {
                game.Players =
                [
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = targetPlayerId;
                        p.Role = PlayerRole.Manager;
                        p.TeamId = fixture.Create<string>();
                        p.User = state.Build<Data.User>(fixture);
                    })
                ];
            });
        });

        // when the first player tries to start the second's session
        var response = await _testContext
            .CreateHttpClientWithAuthRole(UserRoleKey.Admin)
            .PutAsync($"api/player/{targetPlayerId}/start", null);

        // then the response should have a failure code
        response.IsSuccessStatusCode.ShouldBeTrue();
    }
}
