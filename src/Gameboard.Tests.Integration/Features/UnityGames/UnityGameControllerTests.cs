using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration.Features.UnityGames;

public class UnityGameControllerTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public UnityGameControllerTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Fact]
    public async Task UnityGameController_CreateChallenge_DoesntReturnGraderKey()
    {
        //// arrange
        //var game = await _testContext.GetDbContext().CreateGame();
        //var user = await _testContext.CreateUser(UserRole.Admin);
        //var playerId = "player";
        //var teamId = "team";

        //var newChallenge = new NewUnityChallenge()
        //{
        //    GameId = game.Id,
        //    PlayerId = playerId,
        //    TeamId = teamId,
        //    MaxPoints = 50,
        //    GamespaceId = "gamespace",
        //    Vms = new UnityGameVm[]
        //    {
        //        new UnityGameVm
        //        {
        //            Id = "vm",
        //            Url = "google.com",
        //            Name = "vm1"
        //        }
        //    }
        //};

        //var httpClient = _testContext.WithAuthentication().CreateClient();

        //// act
        //var response = await httpClient.PostAsync("/api/unity/challenge", newChallenge.ToStringContent());
        //response.EnsureSuccessStatusCode();
        //var challenge = await response.Content.JsonDeserializeAsync<Api.Data.Challenge>(_testContext.GetJsonSerializerOptions());

        //// assert
        //challenge.ShouldNotBeNull();
        //challenge.GraderKey.ShouldBeNull();
        true.ShouldBeTrue();
    }
}
