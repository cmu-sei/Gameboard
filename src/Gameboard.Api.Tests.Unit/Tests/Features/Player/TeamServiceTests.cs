using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Teams;

namespace Gameboard.Api.Tests.Unit;

public class TeamServiceTests
{
    [Fact]
    public void ResolveCaptain_WhenMultiplePlayersFromSameTeam_ResolvesExpected()
    {
        // arrange
        var mapper = A.Fake<IMapper>();
        var sut = new TeamService(A.Fake<IMapper>(), A.Fake<INowService>(), A.Fake<IInternalHubBus>(), A.Fake<IPlayerStore>());

        var players = new Api.Player[]
        {
            new Api.Player
            {
                Name = "The manager",
                Role = Api.PlayerRole.Manager,
                TeamId = "team"
            },
            new Api.Player
            {
                Name = "The member",
                Role = Api.PlayerRole.Member,
                TeamId = "team"
            }
        }.AsEnumerable();

        // act
        var result = sut.ResolveCaptain("team", players);

        // assert
        result.Name.ShouldBe("The manager");
    }
}
