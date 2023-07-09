using Gameboard.Api;
using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class PlayerControllerTests
{
    private readonly GameboardTestContext _testContext;

    public PlayerControllerTests(GameboardTestContext testContext)
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

    [Theory, GbIntegrationAutoData]
    public async Task GetCertificates_WhenScoreConstrained_ReturnsExpectedCount
    (
        int score,
        string scoringUserId,
        string scoringPlayerId,
        string nonScoringUserId,
        string nonScoringPlayerId
    )
    {
        // given
        var now = DateTimeOffset.UtcNow;

        await _testContext.WithDataState(state =>
        {
            state.AddGame(g =>
            {
                g.GameEnd = now - TimeSpan.FromDays(1);
                g.CertificateTemplate = "This is a template with a {{player_count}}.";
                g.Players = new Data.Player[]
                {
                    // i almost broke my brain trying to get GbIntegrationAutoData to work with
                    // inline autodata, so I'm just doing two checks here
                    state.BuildPlayer(p =>
                    {
                        p.Id = scoringPlayerId;
                        p.User = new Data.User { Id = scoringUserId };
                        p.UserId = scoringUserId;
                        p.SessionEnd = now - TimeSpan.FromDays(-2);
                        p.TeamId = "teamId";
                        p.Score = score;
                    }),
                    state.BuildPlayer(p =>
                    {
                        p.Id = nonScoringPlayerId;
                        p.User = new Data.User { Id = nonScoringUserId };
                        p.UserId = nonScoringUserId;
                        p.SessionEnd = now - TimeSpan.FromDays(-2);
                        p.TeamId = "teamId";
                        p.Score = score;
                    })
                };
            });
        });

        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = scoringUserId);

        // when
        var certs = await httpClient
            .GetAsync("/api/certificates")
            .WithContentDeserializedAs<IEnumerable<PlayerCertificate>>();

        // then
        certs?.Count().ShouldBe(1);
        certs?.First().Player.Id.ShouldBe(scoringPlayerId);
    }

    [Theory, GbIntegrationAutoData]
    public async Task GetCertificates_WithTeamsAndNonScorers_ReturnsExpected(string userId, string playerId)
    {
        // given
        var now = DateTimeOffset.UtcNow;
        var recentDate = DateTime.UtcNow.AddDays(-1);

        await _testContext.WithDataState(state =>
        {
            var allPlayers = new List<Data.Player>
            {
                state.BuildPlayer(p =>
                {
                    p.Id = playerId;
                    p.User = new Data.User { Id = userId };
                    p.UserId = userId;
                    p.SessionEnd = recentDate;
                    p.Score = 20;
                })
            };

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

        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = userId);

        // when
        var certs = await httpClient
            .GetAsync("/api/certificates")
            .WithContentDeserializedAs<IEnumerable<PlayerCertificate>>();

        // then
        certs?.Count().ShouldBe(1);
        certs?.First().Html.ShouldBe("This is a template with a 6 and a 2.");
    }
}
