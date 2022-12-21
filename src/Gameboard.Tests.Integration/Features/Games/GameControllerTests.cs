using Gameboard.Api;
using Gameboard.Api.Data;
using Gameboard.Tests.Integration.Fixtures;

namespace Gameboard.Tests.Integration;

public class GameControllerTests : IClassFixture<GameboardTestContext<Program, GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<Program, GameboardDbContextPostgreSQL> _testContext;

    public GameControllerTests(GameboardTestContext<Program, GameboardDbContextPostgreSQL> testContext)
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
        var response = await _testContext.Http.PostAsync("/api/game", game.ToStringContent());

        // assert
        response.EnsureSuccessStatusCode();

        var responseGame = await response.Content.JsonDeserializeAsync<Api.Data.Game>(_testContext.GetJsonSerializerOptions());
        Assert.Equal(game.Name, responseGame?.Name);
    }
}
