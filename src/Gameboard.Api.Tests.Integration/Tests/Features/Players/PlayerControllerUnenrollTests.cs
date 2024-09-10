using System.Net;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class PlayerControllerUnenrollTests : IClassFixture<GameboardTestContext>
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
                state.Add<Data.Game>(fixture, g =>
                {
                    g.Players =
                    [
                        state.Build<Data.Player>(fixture, p =>
                        {
                            p.Id = fixture.Create<string>();
                            p.Name = "A";
                            p.TeamId = teamId;
                            p.Role = PlayerRole.Manager;
                        }),

                        state.Build<Data.Player>(fixture, p =>
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
                            p.Challenges = state.Build<Data.Challenge>(fixture, c =>
                            {
                                // the challenge is associated with the player but no other team
                                // so it should get deleted
                                c.Id = challengeId;
                                c.PlayerId = memberPlayerId;
                                c.TeamId = teamId;
                            }).ToCollection();
                        })
                    ];
                });
            });

        var reqParams = new PlayerUnenrollRequest { PlayerId = memberPlayerId };

        // when
        var response = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = memberUserId)
            .DeleteAsync($"/api/player/{memberPlayerId}?{reqParams.ToQueryString()}");

        // then
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var dbContext = _testContext.GetValidationDbContext();
        var hasPlayer = await dbContext.Players.AnyAsync(p => p.Id == memberPlayerId);
        var hasChallenge = await dbContext.Challenges.AnyAsync(c => c.Id == challengeId);

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
                state.Add<Data.Game>(fixture, g =>
                {
                    g.Players = new List<Data.Player>
                    {
                        state.Build<Data.Player>(fixture, p =>
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

                        state.Build<Data.Player>(fixture, p =>
                        {
                            p.Role = PlayerRole.Member;
                            p.TeamId = "team";
                        })
                    };
                });
            });

        var reqParams = new PlayerUnenrollRequest { PlayerId = managerPlayerId };

        // when / then
        var response = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = managerUserId)
            .DeleteAsync($"/api/player/{managerPlayerId}?{reqParams.ToQueryString()}");

        // then
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
