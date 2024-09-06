using System.Net;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class TeamsControllerResetTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    // these two tests should really be one with inline data, but then you have to specify all the args
    // and that feels weird and i don't like it
    [Theory, GbIntegrationAutoData]
    public async Task Reset_WithNoUnenroll_ArchivesChallenges
    (
        string challengeSpecId,
        string gameId,
        string teamId,
        IFixture fixture
    )
    {
        // given a team with a challenge and a started session
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.ChallengeSpec>(fixture, spec => spec.Id = challengeSpecId);

            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.Players =
                [
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.SessionBegin = DateTimeOffset.UtcNow.AddHours(-1);
                        p.SessionEnd = DateTimeOffset.UtcNow.AddHours(1);
                        p.TeamId = teamId;
                        p.Challenges =
                        [
                            state.Build<Data.Challenge>(fixture, c =>
                            {
                                c.GameId = gameId;
                                c.TeamId = teamId;
                            })
                        ];
                    })
                ];
            });
        });

        // when the team's session is reset
        var response = await _testContext
            .CreateHttpClientWithAuthRole(UserRole.Tester)
            .PutAsync($"api/team/{teamId}/session", new ResetTeamSessionCommand(teamId, TeamSessionResetType.ArchiveChallenges, null).ToJsonBody());

        // we expect a successful request and no challenges in the DB
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var teamChallengeCount = await _testContext
            .GetDbContext()
            .Challenges
            .AsNoTracking()
            .Where(c => c.TeamId == teamId)
            .CountAsync();
        teamChallengeCount.ShouldBe(0);
    }

    [Theory, GbIntegrationAutoData]
    public async Task Reset_WithUnenroll_ArchivesChallenges
    (
        string challengeSpecId,
        string gameId,
        string teamId,
        IFixture fixture
    )
    {
        // given a team with a challenge and a started session
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.ChallengeSpec>(fixture, spec => spec.Id = challengeSpecId);

            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.Players =
                [
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.SessionBegin = DateTimeOffset.UtcNow.AddHours(-1);
                        p.SessionEnd = DateTimeOffset.UtcNow.AddHours(1);
                        p.TeamId = teamId;
                        p.Challenges =
                        [
                            state.Build<Data.Challenge>(fixture, c =>
                            {
                                c.GameId = gameId;
                                c.TeamId = teamId;
                            })
                        ];
                    })
                ];
            });
        });

        // when the team's session is reset
        var response = await _testContext
            .CreateHttpClientWithAuthRole(UserRole.Tester)
            .PutAsync($"api/team/{teamId}/session", new ResetTeamSessionCommand(teamId, TeamSessionResetType.UnenrollAndArchiveChallenges, null).ToJsonBody());

        // we expect a successful request and no challenges in the DB
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var teamChallengeCount = await _testContext
            .GetDbContext()
            .Challenges
            .AsNoTracking()
            .Where(c => c.TeamId == teamId)
            .CountAsync();
        teamChallengeCount.ShouldBe(0);
    }
}
