using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Tests.Integration;

public class UserControllerUpdateTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public UserControllerUpdateTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task Update_WithSponsorChangeAndRegistrations_UpdatesUnplayedRegistrations
    (
        string userId,
        string userName,
        string playedPlayerId,
        string unplayedPlayerId,
        string oldSponsorId,
        string newSponsorId,
        IFixture fixture
    )
    {
        // given a user with two player records (one with a started session, one without)
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Sponsor>(fixture, s => s.Id = oldSponsorId);
            state.Add<Data.Sponsor>(fixture, s => s.Id = newSponsorId);

            state.Add<Data.User>(fixture, u =>
            {
                u.Id = userId;
                u.SponsorId = oldSponsorId;
                u.Name = userName;
                u.ApprovedName = userName;
                u.Role = UserRoleKey.Member;
                u.Enrollments = new List<Data.Player>
                {
                    new()
                    {
                        Id = playedPlayerId,
                        SponsorId = oldSponsorId,
                        SessionBegin = DateTimeOffset.UtcNow
                    },
                    new()
                    {
                        Id = unplayedPlayerId,
                        SponsorId = oldSponsorId
                    }
                };
            });
        });

        // when this user updates their sponsor
        var changedUser = new UpdateUser
        {
            Id = userId,
            SponsorId = newSponsorId
        };

        await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PutAsync("api/user/", changedUser.ToJsonBody());

        // then the sponsor for the played player record should remain the same, but the 
        // sponsor for the unplayed one should change
        var playerRecords = await _testContext
            .GetValidationDbContext()
            .Players
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToArrayAsync();

        playerRecords.Single(p => p.SessionBegin > DateTimeOffset.MinValue).SponsorId.ShouldBe(oldSponsorId);
        playerRecords.Single(p => p.SessionBegin == DateTimeOffset.MinValue).SponsorId.ShouldBe(newSponsorId);
    }
}
