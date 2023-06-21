using Gameboard.Api.Data;
using Gameboard.Api.Features.Reports;

namespace Gameboard.Api.Tests.Unit;

public class EnrollmentReportServiceTests
{
    [Theory, GameboardAutoData]
    public async Task GetRecords_WithOneNonTeamRecord_ReportsExpectedValues(IFixture fixture)
    {
        // given 
        var sponsors = new List<Data.Sponsor>
        {
            new Data.Sponsor
            {
                Id = "good-people",
                Name = "The Good People",
                Logo = "good-people.jpg"
            }
        }.BuildMock();

        var challenge = fixture.Create<Data.Challenge>();
        challenge.Points = 50;
        challenge.Score = 50;

        var player = fixture.Create<Data.Player>();
        player.Challenges = new Data.Challenge[] { challenge };
        player.Game.PlayerMode = PlayerMode.Competition;
        player.Sponsor = sponsors.First().Logo;

        var players = new List<Data.Player> { player }.BuildMock();

        var reportsService = A.Fake<IReportsService>();
        A.CallTo(() => reportsService.ParseMultiSelectCriteria(string.Empty))
            .WithAnyArguments()
            .Returns(Array.Empty<string>());

        var store = A.Fake<IStore>();
        A.CallTo(() => store.List<Data.Sponsor>(false)).Returns(sponsors);
        A.CallTo(() => store.List<Data.Player>(false)).Returns(players);

        var sut = new EnrollmentReportService(reportsService, store);

        // when
        var records = await sut.GetRecords(new EnrollmentReportParameters(), CancellationToken.None);

        // then
        records.Count().ShouldBe(1);
        records.First().Team.ShouldBeNull();
    }

    [Theory, GameboardAutoData]
    public async Task GetRecords_WithOneTeamRecord_ReportsExpectedValues(IFixture fixture)
    {
        // given 
        var sponsors = new List<Data.Sponsor>
        {
            new Data.Sponsor
            {
                Id = "good-people",
                Name = "The Good People",
                Logo = "good-people.jpg",
                Approved = true
            },
            new Data.Sponsor
            {
                Id = "bad-eggs",
                Name = "The Bad Eggs",
                Logo = "bad-eggs.jpg",
                Approved = true
            }
        }.BuildMock();

        var challenge = fixture.Create<Data.Challenge>();
        challenge.Points = 50;
        challenge.Score = 50;

        var player1 = fixture.Create<Data.Player>();
        player1.Challenges = new Data.Challenge[] { challenge };
        player1.Game.PlayerMode = PlayerMode.Competition;
        player1.Sponsor = sponsors.First().Logo;
        player1.Role = PlayerRole.Manager;

        var player2 = fixture.Create<Data.Player>();
        player2.Game = player1.Game;
        player2.GameId = player1.GameId;
        player2.TeamId = player1.TeamId;
        player2.Sponsor = "bad-eggs.jpg";

        var players = new List<Data.Player> { player1, player2 }.BuildMock();

        var reportsService = A.Fake<IReportsService>();
        A.CallTo(() => reportsService.ParseMultiSelectCriteria(string.Empty))
            .WithAnyArguments()
            .Returns(Array.Empty<string>());

        var store = A.Fake<IStore>();
        A.CallTo(() => store.List<Data.Sponsor>(false)).Returns(sponsors);
        A.CallTo(() => store.List<Data.Player>(false)).Returns(players);

        var sut = new EnrollmentReportService(reportsService, store);

        // when
        var records = await sut.GetRecords(new EnrollmentReportParameters(), CancellationToken.None);

        // then
        records.Count().ShouldBe(2);
        records.First().Team.Sponsors.Count().ShouldBe(2);
        records.SelectMany(r => r.Challenges).DistinctBy(c => c.SpecId).Count().ShouldBe(1);
    }
}
