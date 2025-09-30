// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Data;
using Gameboard.Api.Features.Reports;

namespace Gameboard.Api.Tests.Integration;

public class PlayersReportTests : IClassFixture<GameboardTestContext>
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
        var sessionDate = new DateTimeOffset(new DateTime(2023, 12, 19)).ToUniversalTime();
        var olderDate = new DateTimeOffset(new DateTime(2023, 12, 11)).ToUniversalTime();

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
                        p.SessionBegin = sessionDate;
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
            .CreateHttpClientWithAuthRole(UserRoleKey.Admin)
            .GetAsync("/api/reports/players")
            .DeserializeResponseAs<ReportResults<PlayersReportStatSummary, PlayersReportRecord>>();

        // then the user's lastplayedon date should be the more recent one
        results.Records.Single(r => r.User.Id == userId).LastPlayedOn.ShouldBe(sessionDate);
    }
}
