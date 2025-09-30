// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Reports;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure;
using Microsoft.EntityFrameworkCore;
using ServiceStack;

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
                MinTeamSize = 1,
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

        // then the session should have successfully begun
        var sessionStartTime = await _testContext
            .GetValidationDbContext()
            .Players
            .AsNoTracking()
            .Where(p => p.Id == targetPlayerId)
            .Select(p => p.SessionBegin)
            .SingleOrDefaultAsync();

        sessionStartTime.ShouldNotBe(DateTimeOffset.MinValue);
    }

    // Users can team up, leave the team, join a different team, then start sessions on the original and the new team
    [Theory, GbIntegrationAutoData]
    public async Task Team_WhenTeamedUpThenLeave_CanBothStart
    (
        string gameId,
        string player1Id,
        string player2Id,
        string teamId,
        string user1Id,
        string user2Id,
        IFixture fixture
    )
    {
        // given two players
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Game>(fixture, game =>
            {
                game.Id = gameId;
                game.MaxTeamSize = 2;
                game.Players =
                [
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = player1Id;
                        p.Role = PlayerRole.Manager;
                        p.TeamId = teamId;
                        p.User = state.Build<Data.User>(fixture, u => u.Id = user1Id);
                    }),
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = player2Id;
                        p.Role = PlayerRole.Manager;
                        p.TeamId = fixture.Create<string>();
                        p.User = state.Build<Data.User>(fixture, u => u.Id = user2Id);
                    })
                ];
            });
        });

        // when they team up, the second player leaves, and both try to start their sessions
        var player1Http = _testContext.CreateHttpClientWithActingUser(u => u.Id = user1Id);
        var player2Http = _testContext.CreateHttpClientWithActingUser(u => u.Id = user2Id);

        // generate invite
        var inviteCode = await player1Http
            .PostAsync($"api/player/{player1Id}/invite", null)
            .DeserializeResponseAs<TeamInvitation>();

        // team up
        var player2 = await player2Http
            .PostAsync($"api/player/enlist", new PlayerEnlistment
            {
                Code = inviteCode.Code,
                PlayerId = player2Id,
                UserId = user2Id
            }.ToJsonBody())
            .DeserializeResponseAs<Player>();

        // player 2 unenrolls
        await player2Http.DeleteAsync($"api/player/{player2Id}");

        // player 2 re-enrolls on a different team
        var player2ReEnrolled = await player2Http.PostAsync($"api/player", new NewPlayer
        {
            GameId = gameId,
            UserId = user2Id
        }.ToJsonBody())
        .DeserializeResponseAs<Player>();

        // both sessions launch
        var response1 = await player1Http.PutAsync($"api/player/{player1Id}/start", null);
        var response2 = await player2Http.PutAsync($"api/player/{player2ReEnrolled.Id}/start", null);

        // both users should have started sessions
        var finalPlayers = await _testContext
            .GetValidationDbContext()
            .Players
            .AsNoTracking()
            .Where(p => p.GameId == gameId)
            .ToArrayAsync();

        finalPlayers.ShouldContain(p => p.UserId == user1Id && p.SessionBegin != DateTimeOffset.MinValue);
        finalPlayers.ShouldContain(p => p.UserId == user2Id && p.SessionBegin != DateTimeOffset.MinValue);
    }
}
