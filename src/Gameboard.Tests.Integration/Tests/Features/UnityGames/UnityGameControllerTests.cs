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
        await _testContext
            .WithDataState(state =>
            {
                state.Add(state.BuildPlayer(), p =>
                {
                    p.Id = "playerId";
                    p.Game = state.BuildGame(g =>
                    {
                        g.Id = "gameId";
                        g.Specs = new List<ChallengeSpec> { state.BuildChallengeSpec() };
                    });
                    p.TeamId = "teamId";
                });
            });

        var newChallenge = new NewUnityChallenge()
        {
            GameId = "gameId",
            PlayerId = "playerId",
            TeamId = "teamId",
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

        var httpClient = _testContext.CreateHttpClientWithAuth();

        // act
        var challenge = await httpClient
            .PostAsync("/api/unity/challenge", newChallenge.ToJsonBody())
            .WithContentDeserializedAs<Api.Data.Challenge>(_testContext.GetJsonSerializerOptions());

        // assert
        challenge.ShouldNotBeNull();
        challenge.GraderKey.ShouldBeNull();
    }
}
