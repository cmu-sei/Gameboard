using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Tests.Integration.Teams;

public class TeamControllerGetTeamsTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;
    public TeamControllerGetTeamsTests(GameboardTestContext testContext) => _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task GetTeams_WithSingleTeamOfTwo_ReturnsExpected
    (
        string gameId,
        string teamId,
        DateTimeOffset sessionEnd,
        DateTimeOffset wrongSessionEnd,
        IFixture fixture
    )
    {
        var universalSessionEnd = sessionEnd.ToUniversalTime();

        // given a team with two people
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.Players = new List<Data.Player>()
                {
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.TeamId = teamId;
                        p.SessionEnd = wrongSessionEnd.ToUniversalTime();
                        p.Role = PlayerRole.Member;
                    }),

                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.TeamId = teamId;
                        p.SessionEnd = universalSessionEnd;
                        p.Role = PlayerRole.Manager;
                    })
                };
            });
        });

        // when we ask for the team by Id
        var result = await _testContext
            .CreateHttpClientWithAuthRole(UserRole.Observer)
            .GetAsync($"api/team/{teamId}")
            .WithContentDeserializedAs<Team>();

        // we should get back one team with two members
        result.TeamId.ShouldBe(teamId);
        result.Members.Count().ShouldBe(2);
        // and we should be respecting the captain's end time
        result.SessionEnd.ToUniversalTime().ShouldBe(universalSessionEnd, TimeSpan.FromMilliseconds(100));
    }

    [Theory, GbIntegrationAutoData]
    public async Task GetTeams_WithMultipleTeamIds_ReturnsExpected
    (
        string gameId,
        string teamId,
        string otherTeamId,
        IFixture fixture
    )
    {
        // given a team with two people
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Game>(fixture, g =>
            {
                g.Id = gameId;
                g.Players = new List<Data.Player>()
                {
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.TeamId = teamId;
                        p.Role = PlayerRole.Member;
                    }),

                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.TeamId = teamId;
                        p.Role = PlayerRole.Manager;
                    }),

                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.TeamId = otherTeamId;
                        p.Role = PlayerRole.Manager;
                    })
                };
            });
        });

        // when we ask for the team by Id
        var result = await _testContext
            .CreateHttpClientWithAuthRole(UserRole.Support)
            .GetAsync($"api/admin/team/search?teamIds={teamId},{otherTeamId}")
            .WithContentDeserializedAs<IEnumerable<Team>>();

        // we should get back two tames
        result.Count().ShouldBe(2);
        result.Single(t => t.TeamId == teamId).Members.Count().ShouldBe(2);
    }
}
