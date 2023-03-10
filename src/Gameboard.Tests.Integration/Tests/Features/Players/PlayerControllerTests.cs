using Gameboard.Api;
using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration;

public class PlayerControllerTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public PlayerControllerTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Fact]
    public async Task Update_WhenNameNotUniqueInGame_SetsNameNotUnique()
    {
        // given
        await _testContext
            .WithDataState(state =>
            {
                state.AddGame(g =>
                {
                    g.Players = new Api.Data.Player[]
                    {
                        state.BuildPlayer(p =>
                        {
                            p.Id = "PlayerA";
                            p.Name = "A";
                            p.TeamId = "team A";
                        }),

                        state.BuildPlayer(p =>
                        {
                            p.Id = "PlayerB";
                            p.Name = "B";
                            p.TeamId = "team B";
                        })
                    };
                });
            });

        var httpClient = _testContext.CreateHttpClientWithAuthRole(UserRole.Admin);
        var sutParams = new ChangedPlayer
        {
            Id = "PlayerB",
            // tries to update `playerB` to have the same name as `playerA`
            Name = "A",
            ApprovedName = "B",
            Sponsor = "sponsor",
            Role = PlayerRole.Member
        };

        // when
        var updatedPlayer = await httpClient
            .PutAsync("/api/player", sutParams.ToJsonBody())
            .WithContentDeserializedAs<Api.Player>();

        // assert
        updatedPlayer?.NameStatus.ShouldBe(AppConstants.NameStatusNotUnique);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task GetCertificates_WhenScoreConstrained_ReturnsExpectedCount(int score)
    {
        // given
        var now = DateTimeOffset.UtcNow;
        var userId = TestIds.Generate();

        await _testContext.WithDataState(state =>
        {
            state.AddGame(g =>
            {
                g.GameEnd = now - TimeSpan.FromDays(1);
                g.CertificateTemplate = "This is a template with a {{player_count}}.";
                g.Players = new Api.Data.Player[]
                {
                    state.BuildPlayer(p =>
                    {
                        p.Id = TestIds.Generate();
                        p.User = new Api.Data.User { Id = userId };
                        p.UserId = userId;
                        p.SessionEnd = now - TimeSpan.FromDays(-2);
                        p.TeamId = "teamId";
                        p.Score = score;
                    })
                };
            });
        });

        var httpClient = _testContext.CreateHttpClientWithAuth(u => u.Id = userId);

        // when
        var certs = await httpClient
            .GetAsync("/api/certificates")
            .WithContentDeserializedAs<IEnumerable<PlayerCertificate>>();

        // then
        certs?.Count().ShouldBe(score == 0 ? 0 : 1);
    }

    [Fact]
    public async Task GetCertificates_WithTeamsAndNonScorers_ReturnsExpected()
    {
        // given
        var now = DateTimeOffset.UtcNow;
        var userId = TestIds.Generate();
        var playerId = TestIds.Generate();
        var recentDate = DateTime.UtcNow.AddDays(-1);

        await _testContext.WithDataState(state =>
        {
            var allPlayers = new List<Api.Data.Player>();

            allPlayers.Add(state.BuildPlayer(p =>
            {
                p.Id = TestIds.Generate();
                p.User = new Api.Data.User { Id = userId };
                p.UserId = userId;
                p.SessionEnd = recentDate;
                p.Score = 20;
            }));

            allPlayers.AddRange(state.BuildTeam(playerBuilder: p =>
            {
                p.Score = 5;
                p.SessionEnd = recentDate;
            }));

            allPlayers.Add(state.BuildPlayer(p =>
            {
                p.SessionEnd = recentDate;
                p.Score = 0;
            }));

            state.AddGame(g =>
            {
                g.GameEnd = now - TimeSpan.FromDays(1);
                g.CertificateTemplate = "This is a template with a {{player_count}} and a {{team_count}}.";
                g.Players = allPlayers;
            });
        });

        var httpClient = _testContext.CreateHttpClientWithAuth(u => u.Id = userId);

        // when
        var certs = await httpClient
            .GetAsync("/api/certificates")
            .WithContentDeserializedAs<IEnumerable<PlayerCertificate>>();

        // then
        certs?.Count().ShouldBe(1);
        certs?.First().Html.ShouldBe("This is a template with a 6 and a 2.");
    }
}
