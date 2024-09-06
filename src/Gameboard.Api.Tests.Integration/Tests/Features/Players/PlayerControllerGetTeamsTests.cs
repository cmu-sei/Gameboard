using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Tests.Integration;

public class PlayerControllerGetTeamsTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task GetTeams_OnNormalRequest_DoesntThrow(string gameId, IFixture fixture)
    {
        // given a game with a player with associated user and sponsor
        await _testContext.WithDataState(state =>
        {
            var sponsor = state.Build<Data.Sponsor>(fixture);

            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.Players = state.Build<Data.Player>(fixture, p =>
                {
                    p.Sponsor = sponsor;
                    p.User = state.Build<Data.User>(fixture, u => u.Sponsor = sponsor);
                }).ToCollection();
            });
        });

        // when the game's teams are requested
        var result = await _testContext
            .CreateHttpClientWithAuthRole(UserRole.Support)
            .GetAsync($"/api/teams/{gameId}")
            .DeserializeResponseAs<TeamSummary[]>();

        // then we should get a list with one team in it
        result.ShouldNotBeNull();
        result.Length.ShouldBe(1);
    }
}
