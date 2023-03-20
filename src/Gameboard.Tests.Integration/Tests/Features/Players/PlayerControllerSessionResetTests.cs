using System.Net;
using Gameboard.Api;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Tests.Integration;

public class PlayerControllerSessionResetTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public PlayerControllerSessionResetTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task ResetSession_WithTeam_DeletesExpectedData(IFixture fixture, string teamId)
    {
        // given
        await _testContext.WithDataState(s =>
        {
            var teamCreateResult = s.AddTeam(fixture, opts =>
            {
                opts.NumPlayers = 2;
                opts.TeamId = teamId;
            });
        });

        // TODO: have withDataState give you stuff back so you don't have to query
        var somePlayer = await _testContext.GetDbContext().Players.FirstAsync(p => p.TeamId == teamId && p.Role == PlayerRole.Member);
        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = somePlayer.UserId);

        // when 
        var response = await httpClient.DeleteAsync($"api/player/{somePlayer.Id}/session");

        // then
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var hasChallenge = await _testContext.GetDbContext().Challenges.AnyAsync(c => c.TeamId == teamId);
        var hasPlayers = await _testContext.GetDbContext().Players.AnyAsync(p => p.TeamId == teamId);
    }
}
