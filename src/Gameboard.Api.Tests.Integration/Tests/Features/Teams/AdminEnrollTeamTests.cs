using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Tests.Integration;

public class AdminEnrollTeamTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task EnrollTeam_WithTwoEligiblePlayersAndNoSpecifiedCaptain_Enrolls
    (
        string gameId,
        string firstUserId,
        string secondUserId,
        string sponsorId,
        IFixture fixture
    )
    {
        // given two users who have selected a sponsor and have never played and an admin teaming them up
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Sponsor>(fixture, s =>
            {
                s.Id = sponsorId;
                s.Name = fixture.Create<string>();
            });

            state.Add<Data.User>(fixture, u =>
            {
                u.Id = firstUserId;
                u.Role = UserRole.Member;
                u.SponsorId = sponsorId;
            });

            state.Add<Data.User>(fixture, u =>
            {
                u.Id = secondUserId;
                u.Role = UserRole.Member;
                u.SponsorId = sponsorId;
            });

            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.MinTeamSize = 1;
                g.MaxTeamSize = 2;
            });
        });

        // when the registrar tries to team them up
        var result = await _testContext
            .CreateHttpClientWithAuthRole(UserRole.Support)
            .PostAsync("api/admin/team", new AdminEnrollTeamRequest(gameId, [firstUserId, secondUserId]).ToJsonBody())
            .DeserializeResponseAs<AdminEnrollTeamResponse>();

        result.GameId.ShouldBe(gameId);
        result.Players.Count().ShouldBe(2);
    }
}
