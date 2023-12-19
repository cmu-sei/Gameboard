using Gameboard.Api.Features.Reports;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class PlayersReportTests
{
    private readonly GameboardTestContext _testContext;

    public PlayersReportTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task GivenOneMatch_WithMultiplePlayerRecords_SelectsMostRecent(string userId, IFixture fixture)
    {
        // given a single user with two player records
        var recentDate = DateTimeOffset.UtcNow;
        var olderDate = recentDate.AddDays(-7);

        await _testContext.WithDataState(state =>
        {
            state.Add<Data.User>(fixture, u =>
            {
                u.Id = userId;
                u.Sponsor = fixture.Create<Data.Sponsor>();
                u.Enrollments = new List<Data.Player>
                {
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.SessionBegin = recentDate;
                    }),
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.SessionBegin = olderDate;
                    })
                };
            });
        });

        // when the report is run
        var results = await _testContext
            .CreateHttpClientWithAuthRole(UserRole.Admin)
            .GetAsync("/api/reports/players")
            .WithContentDeserializedAs<ReportResults<PlayersReportStatSummary, PlayersReportRecord>>();

        // then the user's lastplayedon date should be the more recent one
        results.Records.Single(r => r.User.Id == userId).LastPlayedOn.ShouldBe(recentDate);
    }
}
