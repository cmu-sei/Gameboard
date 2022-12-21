using Gameboard.Api;
using Gameboard.Api.Data;
using Gameboard.Api.Features.UnityGames;

namespace Gameboard.Tests.Integration.Features.UnityGames;

public class UnityGameControllerTests : IClassFixture<GameboardTestContext<Program, GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<Program, GameboardDbContextPostgreSQL> _testContext;

    public UnityGameControllerTests(GameboardTestContext<Program, GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Fact]
    public async Task UnityGameController_CreateChallenge_DoesntReturnGraderKey()
    {
        // arrange
        var user = await _testContext.CreateUser(UserRole.Admin);
        var gameId = "game";
        var playerId = "player";
        var teamId = "team";

        var newChallenge = new NewUnityChallenge()
        {
            GameId = gameId,
            PlayerId = playerId,
            TeamId = teamId,
            MaxPoints = 50,
            GamespaceId = "gamespace",
            Vms = new UnityGameVm[]
            {
                new UnityGameVm
                {
                    Id = "vm",
                    Url = "google.com",
                    Name = "vm1"
                }
            }
        };

        // act
        var response = await _testContext.Http.PostAsync("/api/unity/challenge", newChallenge.ToStringContent());
        response.EnsureSuccessStatusCode();
        var challenge = await response.Content.JsonDeserializeAsync<Api.Data.Challenge>(_testContext.GetJsonSerializerOptions());

        // assert
        challenge.ShouldNotBeNull();
        challenge.GraderKey.ShouldBeNull();
    }
}
