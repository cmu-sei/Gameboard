using Gameboard.Api;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class GameControllerTests
{
    private readonly GameboardTestContext _testContext;

    public GameControllerTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Fact]
    public async Task GameController_Create_ReturnsGame()
    {
        // arrange
        var game = new NewGame()
        {
            Name = "Test game",
            Competition = "Test competition",
            Season = "1",
            Track = "Individual",
            Sponsor = "Test Sponsor",
            GameStart = DateTimeOffset.UtcNow,
            GameEnd = DateTime.UtcNow + TimeSpan.FromDays(30),
            RegistrationOpen = DateTimeOffset.UtcNow,
            RegistrationClose = DateTime.UtcNow + TimeSpan.FromDays(30),
            RegistrationType = GameRegistrationType.Open
        };

        // act
        var responseGame = await _testContext
            .CreateHttpClientWithAuthRole(UserRole.Designer)
            .PostAsync("/api/game", game.ToJsonBody())
            .WithContentDeserializedAs<Api.Data.Game>();

        // assert
        responseGame?.Name.ShouldBe(game.Name);
    }
}
