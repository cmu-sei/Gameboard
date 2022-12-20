using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Gameboard.Api;
using Gameboard.Tests.Integration.Extensions;
using Gameboard.Tests.Integration.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace Gameboard.Tests.Integration;

public class GameControllerTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly HttpClient _http;
    private readonly IOptions<JsonOptions> _jsonOptions;
    private readonly TestWebApplicationFactory<Program> _appFactory;

    public GameControllerTests(TestWebApplicationFactory<Program> appFactory)
    {
        _appFactory = appFactory;
        _jsonOptions = appFactory.Services.GetRequiredService<IOptions<JsonOptions>>();
        _http = appFactory.CreateClient(new WebApplicationFactoryClientOptions
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
            RegistrationClose = DateTime.UtcNow + TimeSpan.FromDays(30),
            RegistrationType = GameRegistrationType.Open
        };

        // act
        var response = await _http.PostAsync("/api/game", game.ToStringContent());

        // assert
        response.EnsureSuccessStatusCode();

        var responseGame = await response.Content.ReadFromJsonAsync<Game>(options: _jsonOptions.Value.JsonSerializerOptions);
        Assert.Equal(game.Name, responseGame?.Name);
    }
}
