using System.Net;
using Gameboard.Api.Data;
using Gameboard.Api.Tests.Shared;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class PlayerControllerSessionResetTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public PlayerControllerSessionResetTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task ResetSession_WithNoTeamUnenroll_DeletesChallengeData(IFixture fixture, string teamId)
    {
        // given
        TeamBuilderResult? result = null;
        await _testContext
            .WithTestServices(s => s.AddGbIntegrationTestAuth(UserRole.Admin))
            .WithDataState(s =>
            {
                result = s.AddTeam(fixture, opts =>
                {
                    opts.NumPlayers = 1;
                    opts.TeamId = teamId;
                });
            });

        if (result == null)
            throw new GbAutomatedTestSetupException("AddTeam failed to return a result.");

        var player = result.Players.First();

        // when 
        var response = await _testContext.Http.PostAsync($"api/team/{player.TeamId}/session", new SessionResetRequest
        {
            Unenroll = true
        }.ToJsonBody());

        // then
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var hasChallenge = await _testContext.GetDbContext().Challenges.AnyAsync(c => c.TeamId == teamId);
        var hasPlayers = await _testContext.GetDbContext().Players.AnyAsync(p => p.TeamId == teamId);

        hasChallenge.ShouldBeFalse();
        hasPlayers.ShouldBeFalse();
    }

    [Theory, GbIntegrationAutoData]
    public async Task ResetSession_WithNoTeamUnenroll_ArchivesChallenges(IFixture fixture, string teamId)
    {
        // given
        TeamBuilderResult? result = null;
        await _testContext
            .WithTestServices(s => s.AddGbIntegrationTestAuth(UserRole.Admin))
            .WithDataState(s =>
            {
                result = s.AddTeam(fixture, opts =>
                {
                    opts.NumPlayers = 1;
                    opts.TeamId = teamId;
                });
            });

        if (result is null)
            throw new GbAutomatedTestSetupException("AddTeam failed to return a result.");

        var player = result.Players.First();

        // when 
        var response = await _testContext.Http.PostAsync($"api/team/{player.TeamId}/session", new SessionResetRequest
        {
            Unenroll = true
        }.ToJsonBody());

        // then
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var hasArchivedChallenges = await _testContext.GetDbContext().ArchivedChallenges.AnyAsync(c => c.TeamId == teamId);
        hasArchivedChallenges.ShouldBeTrue();
    }

    [Theory, GbIntegrationAutoData]
    public async Task ResetSession_WithNoTeamUnenroll_PreservesTeam(IFixture fixture, string teamId)
    {
        // given
        TeamBuilderResult? result = null;
        await _testContext
            .WithTestServices(s => s.AddGbIntegrationTestAuth(UserRole.Admin))
            .WithDataState(s =>
            {
                result = s.AddTeam(fixture, opts =>
                {
                    opts.NumPlayers = 1;
                    opts.TeamId = teamId;
                });
            });

        if (result is null)
            throw new GbAutomatedTestSetupException("AddTeam failed to return a result.");

        var player = result.Players.First();

        // when 
        var response = await _testContext.Http.PostAsync($"api/team/{player.TeamId}/session", new SessionResetRequest
        {
            Unenroll = false
        }.ToJsonBody());

        // then
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var teamPreserved = (await _testContext.GetDbContext().Players.FirstOrDefaultAsync(p => p.TeamId == teamId));
        teamPreserved.ShouldNotBeNull();
    }

    [Theory, GbIntegrationAutoData]
    public async Task ResetSession_WithAlreadyArchivedChallenges_DoesntChoke(IFixture fixture, string teamId)
    {
        // given
        TeamBuilderResult? result = null;
        await _testContext
            .WithTestServices(s => s.AddGbIntegrationTestAuth(UserRole.Admin))
            .WithDataState(s =>
            {
                result = s.AddTeam(fixture, opts =>
                {
                    opts.NumPlayers = 1;
                    opts.TeamId = teamId;
                });

                s.Add(new Data.ArchivedChallenge
                {
                    Id = result.Challenge!.Id,
                    TeamId = teamId
                });
            });

        if (result is null)
            throw new GbAutomatedTestSetupException("AddTeam failed to return a result.");

        var challenges = await _testContext.GetDbContext().Challenges.Where(c => c.TeamId == teamId).ToListAsync();
        var archivedChallenges = await _testContext.GetDbContext().ArchivedChallenges.Where(c => c.TeamId == teamId).ToListAsync();

        var player = result.Players.First();

        // when / then
        await Should.NotThrowAsync(_testContext.Http.PostAsync($"api/team/{player.TeamId}/session", new SessionResetRequest
        {
            Unenroll = true
        }.ToJsonBody()));

        var archivedChallengeCount = await _testContext.GetDbContext().ArchivedChallenges.Where(c => c.TeamId == teamId).CountAsync();
        archivedChallengeCount.ShouldBe(1);
    }
}
