using Gameboard.Api;
using Gameboard.Api.Services;

namespace Gameboard.Tests.Unit;

public class PlayerServiceTests
{
    [Theory, GameboardAutoData]
    public async Task Standings_WhenGameIdIsEmpty_ReturnsEmptyArray(IFixture fixture)
    {
        // arrange
        var sut = fixture.Create<PlayerService>();
        var filterParams = A.Fake<PlayerDataFilter>();

        // act
        var result = await sut.Standings(filterParams);

        // assert
        result.ShouldBe(new Standing[] { });
    }
}