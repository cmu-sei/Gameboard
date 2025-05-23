namespace Gameboard.Api.Tests.Integration;

public class CertificatesControllerTests_GetCertificatesTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    // [Theory, GbIntegrationAutoData]
    // public async Task GetCertificates_WhenScoreConstrained_ReturnsExpectedCount
    // (
    //     int score,
    //     string scoringUserId,
    //     string scoringPlayerId,
    //     string nonScoringUserId,
    //     string nonScoringPlayerId,
    //     IFixture fixture
    // )
    // {
    //     // given
    //     var now = DateTimeOffset.UtcNow;

    //     await _testContext.WithDataState(state =>
    //     {
    //         state.Add<Data.Game>(fixture, g =>
    //         {
    //             g.GameEnd = now - TimeSpan.FromDays(1);
    //             g.CertificateTemplateLegacy = "This is a template with a {{player_count}}.";
    //             g.Players =
    //             [
    //                 // i almost broke my brain trying to get GbIntegrationAutoData to work with
    //                 // inline autodata, so I'm just doing two checks here
    //                 state.Build<Data.Player>(fixture, p =>
    //                 {
    //                     p.Id = scoringPlayerId;
    //                     p.User = state.Build<Data.User>(fixture, u => u.Id = scoringUserId);
    //                     p.UserId = scoringUserId;
    //                     p.SessionEnd = now - TimeSpan.FromDays(-2);
    //                     p.TeamId = "teamId";
    //                     p.Score = score;
    //                 }),
    //                 state.Build<Data.Player>(fixture, p =>
    //                 {
    //                     p.Id = nonScoringPlayerId;
    //                     p.User = state.Build<Data.User>(fixture, u => u.Id = nonScoringUserId);
    //                     p.UserId = nonScoringUserId;
    //                     p.SessionEnd = now - TimeSpan.FromDays(-2);
    //                     p.TeamId = "teamId";
    //                     p.Score = score;
    //                 })
    //             ];
    //         });
    //     });

    //     var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = scoringUserId);

    //     // when
    //     var certs = await httpClient
    //         .GetAsync("/api/certificates")
    //         .DeserializeResponseAs<IEnumerable<PlayerCertificate>>();

    //     // then
    //     certs?.Count().ShouldBe(1);
    //     certs?.First().Player.Id.ShouldBe(scoringPlayerId);
    // }

    // [Theory, GbIntegrationAutoData]
    // public async Task GetCertificates_WithTeamsAndNonScorers_ReturnsExpected(string teamId, string userId, IFixture fixture)
    // {
    //     // given
    //     var now = DateTimeOffset.UtcNow;
    //     var recentDate = DateTime.UtcNow.AddDays(-1);

    //     await _testContext.WithDataState(state =>
    //     {
    //         state.Add<Data.Game>(fixture, g =>
    //         {
    //             g.CertificateTemplateLegacy = "This is a template with a {{player_count}} and a {{team_count}}.";
    //             g.GameEnd = now - TimeSpan.FromDays(1);
    //             g.Players = new List<Data.Player>
    //             {
    //                 // three players with nonzero score (2 on the same team)
    //                 state.Build<Data.Player>(fixture, p =>
    //                 {
    //                     p.SessionEnd = recentDate;
    //                     p.Score = 20;
    //                     p.User = state.Build<Data.User>(fixture, u => u.Id = userId);
    //                 }),
    //                 state.Build<Data.Player>(fixture, p =>
    //                 {
    //                     p.SessionEnd = recentDate;
    //                     p.Score = 30;
    //                     p.TeamId = teamId;
    //                 }),
    //                 state.Build<Data.Player>(fixture, p =>
    //                 {
    //                     p.SessionEnd = recentDate;
    //                     p.Score = 30;
    //                     p.TeamId = teamId;
    //                 }),
    //                 // one player with zero score
    //                 state.Build<Data.Player>(fixture, p =>
    //                 {
    //                     p.SessionEnd = recentDate;
    //                     p.Score = 0;
    //                 }),
    //             };
    //         });
    //     });

    //     var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = userId);

    //     // when
    //     var certsResponse = await httpClient
    //         .GetAsync("/api/certificates")
    //         .DeserializeResponseAs<IEnumerable<PlayerCertificate>>();

    //     // then
    //     var certs = certsResponse.ToArray();
    //     certs.ShouldNotBeNull();
    //     certs.Count().ShouldBe(1);
    //     certs.First().Html.ShouldBe("This is a template with a 3 and a 2.");
    // }
}
