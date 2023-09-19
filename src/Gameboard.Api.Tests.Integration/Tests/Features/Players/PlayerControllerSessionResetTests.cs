using System.Net;
using Fare;
using Gameboard.Api.Common;
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
    public async Task ResetSession_WithManualReset_DeletesChallengeData
    (
        IFixture fixture,
        string playerId,
        string teamId,
        string userId
    )
    {
        // given
        await _testContext.WithDataState(s =>
        {
            s.Add<Data.Player>(fixture, p =>
            {
                p.Id = playerId;
                p.TeamId = teamId;
                p.User = s.Build<Data.User>(fixture, u => u.Id = userId);
            });
        });

        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = userId);

        // when 
        var response = await httpClient.PostAsync($"api/player/{playerId}/session", new SessionResetRequest
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
    public async Task ResetSession_WithManualReset_ArchivesChallenges
    (
        IFixture fixture,
        string playerId,
        string teamId,
        string userId
    )
    {
        // given
        await _testContext.WithDataState(s =>
        {
            s.Add<Data.Player>(fixture, p =>
            {
                p.Id = playerId;
                p.TeamId = teamId;
                p.User = s.Build<Data.User>(fixture, u => u.Id = userId);
                p.Challenges = s.Build<Data.Challenge>(fixture, c => c.TeamId = teamId).ToCollection();
            });
        });

        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = userId);

        // when 
        var response = await httpClient.PostAsync($"api/player/{playerId}/session", new SessionResetRequest
        {
            IsManualReset = true,
            UnenrollTeam = true
        }.ToJsonBody());

        // then
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var hasArchivedChallenges = await _testContext.GetDbContext().ArchivedChallenges.AnyAsync(c => c.TeamId == teamId);
        hasArchivedChallenges.ShouldBeTrue();
    }

    [Theory, GbIntegrationAutoData]
    public async Task ResetSession_WithAlreadyArchivedChallenges_DoesntChoke
    (
        IFixture fixture, 
        string challengeId, 
        string teamId, 
        string playerId,
        string playerUserId
    )
    {
        // given
        await _testContext.WithDataState(s =>
        {
            s.Add<Data.Player>(fixture, p =>
            {
                p.Id = playerId;
                p.TeamId = teamId;
                p.User = s.Build<Data.User>(fixture, u => u.Id = playerUserId);
                p.Challenges = s.Build<Data.Challenge>(fixture, c =>
                {
                    c.Id = challengeId;
                }).ToCollection();
            });
            s.Add<Data.ArchivedChallenge>(fixture,  c  => 
            {
                c.Id = challengeId;
                c.TeamId = teamId;
            });
        });

        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = playerUserId);

        // when / then
        await Should.NotThrowAsync(httpClient.PostAsync($"api/player/{playerId}/session", new SessionResetRequest
        {
            IsManualReset = true,
            UnenrollTeam = false
        }.ToJsonBody()));

        var archivedChallengeCount = await _testContext.GetDbContext().ArchivedChallenges.Where(c => c.TeamId == teamId).CountAsync();
        archivedChallengeCount.ShouldBe(1);
    }
}
