using Gameboard.Api.Data;
using Gameboard.Api.Features.UnityGames;

namespace Gameboard.Tests.Integration;

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
        // arrange
        var player = await _testContext.GetDbContext().CreatePlayer
        (
            gameBuilder: new GameBuilder { WithChallengeSpec = true }
        );

        var newChallenge = new NewUnityChallenge()
        {
            GameId = player.Game.Id,
            PlayerId = player.Id,
            TeamId = player.TeamId,
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

        var httpClient = _testContext.WithAuthentication().CreateClient();

        // act
        var challenge = await httpClient
            .PostAsync("/api/unity/challenge", newChallenge.ToJsonBody())
            .WithContentDeserializedAs<Api.Data.Challenge>(_testContext.GetJsonSerializerOptions());

        // assert
        challenge.ShouldNotBeNull();
        challenge.GraderKey.ShouldBeNull();
    }
}
