using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Practice;
using MediatR;

namespace Gameboard.Api.Tests.Unit;

public class TeamServiceTests
{
    [Fact]
    public void ResolveCaptain_WhenMultiplePlayersFromSameTeam_ResolvesExpected()
    {
        // arrange
        var sut = new TeamService
        (
            A.Fake<IActingUserService>(),
            A.Fake<IGameEngineService>(),
            A.Fake<IMapper>(),
            A.Fake<IMediator>(),
            A.Fake<INowService>(),
            A.Fake<IInternalHubBus>(),
            A.Fake<IPracticeService>(),
            A.Fake<IStore>()
        );

        var players = new Data.Player[]
        {
            new()
            {
                Name = "The manager",
                Role = PlayerRole.Manager,
                TeamId = "team"
            },
            new()
            {
                Name = "The member",
                Role = PlayerRole.Member,
                TeamId = "team"
            }
        }.AsEnumerable();

        // act
        var result = sut.ResolveCaptain(players);

        // assert
        result.Name.ShouldBe("The manager");
    }
}
