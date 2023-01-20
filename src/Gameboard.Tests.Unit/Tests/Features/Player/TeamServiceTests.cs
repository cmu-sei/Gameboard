using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Player;

namespace Gameboard.Tests.Unit;

public class TeamServiceTests
{
    [Fact]
    public async Task Standings_WhenGameIdIsEmpty_ReturnsEmptyArray()
    {
        // arrange
        var playerStore = A.Fake<IPlayerStore>();
        var sut = new TeamService(playerStore);

        var players = new Player[]
        {
            new Player
            {
                Name = "The manager",
                Role = Api.PlayerRole.Manager,
                TeamId = "team"
            },
            new Player
            {
                Name = "The member",
                Role = Api.PlayerRole.Member,
                TeamId = "team"
            }
        }.BuildMock();

        A.CallTo(() => playerStore.List(null)).Returns(players);

        // act
        var result = await sut.ResolveCaptain("team");

        // assert
        result.Name.ShouldBe("The manager");
    }
}