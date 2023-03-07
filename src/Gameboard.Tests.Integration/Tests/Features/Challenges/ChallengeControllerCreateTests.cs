using Gameboard.Api;
using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration;

public class ChallengeControllerCreateTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public ChallengeControllerCreateTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
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

        var client = _testContext.CreateHttpClientWithAuth(u => u.Id = userId);

        // act
        var challenge = await client
            .PostAsync("/api/challenge", model.ToJsonBody())
            .WithContentDeserializedAs<Api.Challenge>();

        // assert
        challenge?.Name.ShouldBe(specName);
    }
}