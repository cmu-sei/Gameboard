using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Reports;

namespace Gameboard.Api.Tests.Unit;

public class EnrollmentReportServiceTests
{
    [Theory, GameboardAutoData]
    public async Task GetResults_WithOnePlayerAndChallenge_ReportsCompleteSolve(IFixture fixture)
    {
        // given 
        var sponsors = new Data.Sponsor
        {
            Id = "good-people",
            Name = "The Good People",
            Logo = "good-people.jpg"
        }
        .ToEnumerable()
        .BuildMock();

        var challenge = fixture.Create<Data.Challenge>();
        challenge.Points = 50;
        challenge.Score = 50;
        challenge.PlayerMode = PlayerMode.Competition;

        var player = fixture.Create<Data.Player>();
        player.Challenges = new Data.Challenge[] { challenge };
        player.Game = fixture.Create<Data.Game>();
        player.Game.PlayerMode = PlayerMode.Competition;
        player.Sponsor = sponsors.First();

        var players = new List<Data.Player> { player }.BuildMock();

        var reportsService = A.Fake<IReportsService>();
        A.CallTo(() => reportsService.ParseMultiSelectCriteria(string.Empty))
            .WithAnyArguments()
            .Returns(Array.Empty<string>());

        var store = A.Fake<IStore>();
        A.CallTo(() => store.WithNoTracking<Data.Sponsor>()).Returns(sponsors);
        A.CallTo(() => store.WithNoTracking<Data.Player>()).Returns(players);

        var sut = new EnrollmentReportService(reportsService, store);

        // when
        var results = await sut.GetRawResults(new EnrollmentReportParameters(), CancellationToken.None);

        // then
        results.Count().ShouldBe(1);
        results.First().ChallengesCompletelySolvedCount.ShouldBe(1);
    }

    [Theory, GameboardAutoData]
    public async Task GetResults_WithOneTeamRecord_ReportsExpectedValues(string gameId, IFixture fixture)
    {
        // given 
        var sponsors = new List<Data.Sponsor>
        {
            new()
            {
                Id = "good-people",
                Name = "The Good People",
                Logo = "good-people.jpg",
                Approved = true
            },
            new()
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
        challenge.PlayerMode = PlayerMode.Competition;
        challenge.GameId = gameId;

        var game = fixture.Create<Data.Game>();
        game.Id = gameId;
        game.PlayerMode = PlayerMode.Competition;

        var player1 = fixture.Create<Data.Player>();
        player1.Challenges = new Data.Challenge[] { challenge };
        player1.Game = game;
        player1.Sponsor = sponsors.First();
        player1.Role = PlayerRole.Manager;

        var player2 = fixture.Create<Data.Player>();
        player2.Challenges = new Data.Challenge[] { challenge };
        player2.Game = player1.Game;
        player2.GameId = player1.GameId;
        player2.TeamId = player1.TeamId;
        player2.Sponsor = sponsors.ToArray()[1];

        var players = new List<Data.Player> { player1, player2 }.BuildMock();

        var reportsService = A.Fake<IReportsService>();
        A.CallTo(() => reportsService.ParseMultiSelectCriteria(string.Empty))
            .WithAnyArguments()
            .Returns(Array.Empty<string>());

        var store = A.Fake<IStore>();
        A.CallTo(() => store.WithNoTracking<Data.Sponsor>()).Returns(sponsors);
        A.CallTo(() => store.WithNoTracking<Data.Player>()).Returns(players);

        var sut = new EnrollmentReportService(reportsService, store);

        // when
        var results = await sut.GetRawResults(new EnrollmentReportParameters(), CancellationToken.None);

        // then
        results.Count().ShouldBe(2);
        results.First().Team.Sponsors.Count().ShouldBe(2);
        results.SelectMany(r => r.Challenges).DistinctBy(c => c.SpecId).Count().ShouldBe(1);
    }

    [Theory, GameboardAutoData]
    public async Task GetResults_WithGameInPracAndChallengeInComp_ReportsOneResult(IFixture fixture)
    {
        // given 
        var sponsors = new List<Data.Sponsor>
        {
            new()
            {
                Id = "good-people",
                Name = "The Good People",
                Logo = "good-people.jpg"
            }
        }.BuildMock();

        var challenge = fixture.Create<Data.Challenge>();
        challenge.Points = 50;
        challenge.Score = 50;
        challenge.PlayerMode = PlayerMode.Competition;

        var player = fixture.Create<Data.Player>();
        player.Challenges = [challenge];
        player.Game = fixture.Create<Data.Game>();
        player.Game.PlayerMode = PlayerMode.Practice;
        player.Sponsor = sponsors.First();

        var players = new List<Data.Player> { player }.BuildMock();

        var reportsService = A.Fake<IReportsService>();
        A.CallTo(() => reportsService.ParseMultiSelectCriteria(string.Empty))
            .WithAnyArguments()
            .Returns([]);

        var store = A.Fake<IStore>();
        A.CallTo(() => store.WithNoTracking<Data.Sponsor>()).Returns(sponsors);
        A.CallTo(() => store.WithNoTracking<Data.Player>()).Returns(players);

        var sut = new EnrollmentReportService(reportsService, store);

        // when
        var results = await sut.GetRawResults(new EnrollmentReportParameters(), CancellationToken.None);

        // then
        results.Count().ShouldBe(1);
    }

    [Theory, GameboardAutoData]
    public async Task GetResults_WithPlayerWithNoChallenges_IsIncluded(IFixture fixture)
    {
        // given 
        var sponsors = new List<Data.Sponsor>
        {
            new()
            {
                Id = "good-people",
                Name = "The Good People",
                Logo = "good-people.jpg"
            }
        }.BuildMock();

        var player = fixture.Create<Data.Player>();
        player.Challenges = Array.Empty<Data.Challenge>();
        player.Game = fixture.Create<Data.Game>();
        player.Sponsor = sponsors.First();
        var players = player.ToEnumerable().BuildMock();

        var reportsService = A.Fake<IReportsService>();
        A.CallTo(() => reportsService.ParseMultiSelectCriteria(string.Empty))
            .WithAnyArguments()
            .Returns([]);

        var store = A.Fake<IStore>();
        A.CallTo(() => store.WithNoTracking<Data.Sponsor>()).Returns(sponsors);
        A.CallTo(() => store.WithNoTracking<Data.Player>()).Returns(players);

        var sut = new EnrollmentReportService(reportsService, store);

        // when
        var results = await sut.GetRawResults(new EnrollmentReportParameters(), CancellationToken.None);

        // then
        results.Count().ShouldBe(1);
    }

}
