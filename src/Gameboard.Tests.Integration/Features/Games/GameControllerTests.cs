using System.Net.Http.Json;
using Gameboard.Api;
using Gameboard.Tests.Integration.Fixtures;

namespace Gameboard.Tests.Integration;

public class GameControllerTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly HttpClient _http;
    private readonly TestWebApplicationFactory<Program> _appFactory;

    public GameControllerTests(TestWebApplicationFactory<Program> appFactory)
    {
        _appFactory = appFactory;
        _http = appFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
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
            RegistrationClose = DateTime.UtcNow + TimeSpan.FromDays(30)
        };

        // act
        var response = await _http.PostAsync("/api/game", JsonContent.Create(game));

        // assert
        response.EnsureSuccessStatusCode();

        var responseGame = await response.Content.ReadFromJsonAsync<Game>();
        Assert.Equal(game.Name, responseGame?.Name);
    }
}
