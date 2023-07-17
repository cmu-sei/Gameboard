using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Tests.Unit;

public class TeamServiceTests
{
    [Fact]
    public void ResolveCaptain_WhenMultiplePlayersFromSameTeam_ResolvesExpected()
    {
        // arrange
        var sut = new TeamService(A.Fake<IMapper>(), A.Fake<INowService>(), A.Fake<IInternalHubBus>(), A.Fake<IStore>());

        var players = new Api.Player[]
        {
            new Player
            {
                Name = "The manager",
                Role = PlayerRole.Manager,
                TeamId = "team"
            },
            new Player
            {
                Name = "The member",
                Role = PlayerRole.Member,
                TeamId = "team"
            }
        }.AsEnumerable();

        // act
        var result = sut.ResolveCaptain("team", players);

        // assert
        result.Name.ShouldBe("The manager");
    }
}
