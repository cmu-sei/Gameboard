using System.Net;
using Gameboard.Api.Data;
using Gameboard.Api.Tests.Shared;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class PlayerControllerSessionResetTests
{
    private readonly GameboardTestContext _testContext;

    public PlayerControllerSessionResetTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task ResetSession_WithManualReset_DeletesChallengeData(IFixture fixture, string teamId)
    {
        // given
        TeamBuilderResult? result = null;
        await _testContext.WithDataState(s =>
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
        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = player.UserId);

        // when 
        var response = await httpClient.PostAsync($"api/player/{player.Id}/session", new SessionResetRequest
        {
            IsManualReset = true,
            UnenrollTeam = true
        }.ToJsonBody());

        // then
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var hasChallenge = await _testContext.GetDbContext().Challenges.AnyAsync(c => c.TeamId == teamId);
        var hasPlayers = await _testContext.GetDbContext().Players.AnyAsync(p => p.TeamId == teamId);

        hasChallenge.ShouldBeFalse();
        hasPlayers.ShouldBeFalse();
    }

    [Theory, GbIntegrationAutoData]
    public async Task ResetSession_WithManualReset_ArchivesChallenges(IFixture fixture, string teamId)
    {
        // given
        TeamBuilderResult? result = null;
        await _testContext.WithDataState(s =>
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
        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = player.UserId);

        // when 
        var response = await httpClient.PostAsync($"api/player/{player.Id}/session", new SessionResetRequest
        {
            IsManualReset = true,
            UnenrollTeam = true
        }.ToJsonBody());

        // then
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var hasArchivedChallenges = await _testContext.GetDbContext().ArchivedChallenges.AnyAsync(c => c.TeamId == teamId);
    }

    [Theory, GbIntegrationAutoData]
    public async Task ResetSession_WithAlreadyArchivedChallenges_DoesntChoke(IFixture fixture, string teamId)
    {
        // given
        TeamBuilderResult? result = null;
        await _testContext.WithDataState(s =>
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

        if (result == null)
            throw new GbAutomatedTestSetupException("AddTeam failed to return a result.");

        var realTHings = await _testContext.GetDbContext().Challenges.Where(c => c.TeamId == teamId).ToListAsync();
        var things = await _testContext.GetDbContext().ArchivedChallenges.Where(c => c.TeamId == teamId).ToListAsync();

        var player = result.Players.First();
        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = player.UserId);

        // when / then
        await Should.NotThrowAsync(httpClient.PostAsync($"api/player/{player.Id}/session", new SessionResetRequest
        {
            IsManualReset = true,
            UnenrollTeam = false
        }.ToJsonBody()));

        var archivedChallengeCount = await _testContext.GetDbContext().ArchivedChallenges.Where(c => c.TeamId == teamId).CountAsync();
        archivedChallengeCount.ShouldBe(1);
    }
}
