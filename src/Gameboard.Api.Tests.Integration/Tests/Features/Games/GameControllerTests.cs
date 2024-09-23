using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration;

public class GameControllerTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

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
            .CreateHttpClientWithAuthRole(UserRoleKey.Director)
            .PostAsync("/api/game", game.ToJsonBody())
            .DeserializeResponseAs<Data.Game>();

        // assert
        responseGame?.Name.ShouldBe(game.Name);
    }
}
