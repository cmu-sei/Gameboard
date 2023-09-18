using Gameboard.Api.Common;
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
    public async Task UnityGameController_CreateChallenge_DoesntReturnGraderKey(string playerId, string gameId, string teamId, IFixture fixture)
    {
        // arrange
        await _testContext
            .WithDataState(state =>
            {
                state.Add<Data.Player>(fixture, p =>
                {
                    p.Id = playerId;
                    p.Game = state.Build<Data.Game>(fixture, g =>
                    {
                        g.Id = gameId;
                        g.Specs = state.Build<Data.ChallengeSpec>(fixture).ToCollection();
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
               new()
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
            .WithContentDeserializedAs<Data.Challenge>();

        // assert
        challenge.ShouldNotBeNull();
        challenge.GraderKey.ShouldBeNull();
    }
}
