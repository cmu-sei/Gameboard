using System.Net;
using Gameboard.Api;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class PlayerControllerUnenrollTests
{
    private readonly GameboardTestContext _testContext;

    public PlayerControllerUnenrollTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task Unenroll_WhenIsMember_DeletesPlayerAndChallenges(IFixture fixture, string challengeId, string memberPlayerId, string memberUserId, string teamId)
    {
        // given
        await _testContext
            .WithDataState(state =>
            {
                state.AddGame(g =>
                {
                    g.Players = new Data.Player[]
                    {
                        state.BuildPlayer(fixture, p =>
                        {
                            p.Id = fixture.Create<string>();
                            p.Name = "A";
                            p.TeamId = teamId;
                            p.Role = PlayerRole.Manager;
                        }),

                        state.BuildPlayer(fixture, p =>
                        {
                            p.Id = memberPlayerId;
                            p.Name = "B";
                            p.Role = PlayerRole.Member;
                            p.TeamId = teamId;
                            p.User = state.Build<Data.User>(fixture, u =>
                            {
                                u.Id = memberUserId;
                                u.Role = UserRole.Member;
                            });
                            p.Challenges = new Data.Challenge[]
                            {
                                state.BuildChallenge(c =>
                                {
                                    // the challenge is associated with the player but no other team
                                    // so it should get deleted
                                    c.Id = challengeId;
                                    c.PlayerId = memberPlayerId;
                                    c.TeamId = teamId;
                                })
                            };
                        })
                    };
                });
            });

        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = memberUserId);
        var reqParams = new PlayerUnenrollRequest
        {
            PlayerId = memberPlayerId
        };

        // when
        var response = await httpClient.DeleteAsync($"/api/player/{memberPlayerId}?{reqParams.ToQueryString()}");

        // then
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var hasPlayer = await _testContext.GetDbContext().Players.AnyAsync(p => p.Id == memberPlayerId);
        var hasChallenge = await _testContext.GetDbContext().Challenges.AnyAsync(c => c.Id == challengeId);

        hasPlayer.ShouldBeFalse();
        hasChallenge.ShouldBeFalse();
    }

    [Theory, GbIntegrationAutoData]
    public async Task Unenroll_WhenIsManager_Fails(IFixture fixture)
    {
        // given
        var managerUserId = fixture.Create<string>();
        var managerPlayerId = fixture.Create<string>();

        await _testContext
            .WithDataState(state =>
            {
                state.AddGame(g =>
                {
                    g.Players = new Data.Player[]
                    {
                        state.BuildPlayer(fixture, p =>
                        {
                            p.Id = managerPlayerId;
                            p.TeamId = "team";
                            p.Role = PlayerRole.Manager;
                            p.User = state.Build<Data.User>(fixture, u =>
                            {
                                u.Id = fixture.Create<string>();
                                u.Role = UserRole.Member;
                            });
                        }),

                        state.BuildPlayer(fixture, p =>
                        {
                            p.Role = PlayerRole.Member;
                            p.TeamId = "team";
                        })
                    };
                });
            });

        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = managerUserId);
        var reqParams = new PlayerUnenrollRequest
        {
            PlayerId = managerPlayerId
        };

        // when / then
        var response = await httpClient.DeleteAsync($"/api/player/{managerPlayerId}?{reqParams.ToQueryString()}");

        // then
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
