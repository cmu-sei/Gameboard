using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Tests.Unit;

public class TeamServiceTests
{
    [Fact]
    public async Task Standings_WhenGameIdIsEmpty_ReturnsEmptyArray()
    {
        // arrange
        var playerStore = A.Fake<IPlayerStore>();
        var mapper = A.Fake<IMapper>();
        var sut = new TeamService
        (
            A.Fake<IMapper>(),
            A.Fake<IMemoryCache>(),
            A.Fake<INowService>(),
            A.Fake<IInternalHubBus>(),
            playerStore
        );

        var players = new Data.Player[]
        {
            new Data.Player
            {
                Name = "The manager",
                Role = Api.PlayerRole.Manager,
                TeamId = "team"
            },
            new Data.Player
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
