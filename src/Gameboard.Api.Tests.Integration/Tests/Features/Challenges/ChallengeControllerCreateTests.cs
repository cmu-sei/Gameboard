using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration;

public class ChallengeControllerCreateTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public ChallengeControllerCreateTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task ChallengeControllerCreate_WithMinimal_ReturnsExpectedChallenge
    (
        string challengeSpecId,
        string specName,
        string playerId,
        string userId,
        IFixture fixture)
    {
        // arrange
        await _testContext
            .WithTestServices(services => services.AddGbIntegrationTestAuth(u => u.Id = userId))
            .WithDataState(state =>
            {
                state.AddChallengeSpec(spec =>
                {
                    spec.Id = challengeSpecId;
                    spec.Name = specName;
                    spec.Game = new Api.Data.Game
                    {
                        Id = fixture.Create<string>(),
                        Players = new Api.Data.Player[]
                        {
                            state.BuildPlayer(p =>
                            {
                                p.Id = playerId;
                                p.User = new Api.Data.User { Id = userId };
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

        // act
        var challenge = await _testContext
            .Http
            .PostAsync("/api/challenge", model.ToJsonBody())
            .WithContentDeserializedAs<Api.Challenge>();

        // assert
        challenge?.Name.ShouldBe(specName);
        challenge?.State.ManagerId.ShouldBe(playerId);
    }
}
