using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration;

public class PlayerControllerGetTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public PlayerControllerGetTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task Get_WithTwoTeamedPlayersAndTeamsCollapsed_HasExpectedSponsorLogos(string gameId, string teamId, string sponsor1Logo, string sponsor2Logo, IFixture fixture)
    {
        // given
        await _testContext
            .WithDataState(state =>
            {
                state.Add<Data.Game>(fixture, g =>
                {
                    g.Id = gameId;
                    g.Players = new List<Data.Player>
                    {
                        state.Build<Data.Player>(fixture, p =>
                        {
                            p.TeamId = teamId;
                            p.Sponsor = state.Build<Data.Sponsor>(fixture, s => s.Logo = sponsor1Logo);
                            p.Role = PlayerRole.Manager;
                        }),

                        state.Build<Data.Player>(fixture, p =>
                        {
                            p.TeamId = teamId;
                            p.Sponsor = state.Build<Data.Sponsor>(fixture, s => s.Logo = sponsor2Logo);
                            p.Role = PlayerRole.Member;
                        })
                    };
                });
            });

        // when
        var results = await _testContext
            .CreateHttpClientWithAuthRole(UserRoleKey.Admin)
            .GetAsync($"/api/players?filter=collapse&gid={gameId}")
            .DeserializeResponseAs<Player[]>();

        // assert
        results.Length.ShouldBe(1);
        results.First().TeamSponsorLogos.Length.ShouldBe(2);
    }
}
