using Gameboard.Api;
using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration;

[Collection(TestCollectionNames.DbFixtureTests)]
public class ChallengeControllerCreateTests
{
    private readonly GameboardTestContext _testContext;

    public ChallengeControllerCreateTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task ChallengeControllerCreate_WithMinimal_ReturnsExpectedChallenge(
        string challengeSpecId,
        string specName,
        string playerId,
        string userId,
        IFixture fixture)
    {
        // arrange
        await _testContext.WithDataState(state =>
        {
            state.AddChallengeSpec(spec =>
            {
                spec.Id = challengeSpecId;
                spec.Name = specName;
                spec.Game = new Data.Game
                {
                    Id = fixture.Create<string>(),
                    Players = new Api.Data.Player[]
                    {
                        state.BuildPlayer(p =>
                        {
                            p.Id = playerId;
                            p.User = new Data.User { Id = userId };
                            p.SessionBegin = DateTimeOffset.UtcNow.AddDays(-1);
                            p.SessionEnd = DateTimeOffset.UtcNow.AddDays(1);
                        })
                    }
                };
            });
        });

        var model = new NewChallenge
        {
            SpecId = challengeSpecId,
            PlayerId = playerId,
            Variant = 0
        };

        var client = _testContext.CreateHttpClientWithActingUser(u => u.Id = userId);

        // act
        var challenge = await client
            .PostAsync("/api/challenge", model.ToJsonBody())
            .WithContentDeserializedAs<Api.Challenge>();

        // assert
        challenge?.Name.ShouldBe(specName);
        challenge?.State.ManagerId.ShouldBe(playerId);
    }
}
