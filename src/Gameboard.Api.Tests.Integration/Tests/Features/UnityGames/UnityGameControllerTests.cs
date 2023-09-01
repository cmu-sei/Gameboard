using Gameboard.Api.Data;
using Gameboard.Api.Features.UnityGames;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class UnityGameControllerTests
{
    private readonly GameboardTestContext _testContext;

    public UnityGameControllerTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task UnityGameController_CreateChallenge_DoesntReturnGraderKey(string playerId, string gameId, string teamId)
    {
        // arrange
        await _testContext
            .WithDataState(state =>
            {
                state.Add(state.BuildPlayer(), p =>
                {
                    p.Id = playerId;
                    p.Game = state.BuildGame(g =>
                    {
                        g.Id = gameId;
                        g.Specs = new List<Data.ChallengeSpec> { state.BuildChallengeSpec() };
                    });
                    p.TeamId = teamId;
                });
            });

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

        var httpClient = _testContext.CreateHttpClientWithAuthRole(UserRole.Admin);

        // act
        var challenge = await httpClient
            .PostAsync("/api/unity/challenge", newChallenge.ToJsonBody())
            .WithContentDeserializedAs<Api.Data.Challenge>();

        // assert
        challenge.ShouldNotBeNull();
        challenge.GraderKey.ShouldBeNull();
    }
}
