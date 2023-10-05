using Gameboard.Api.Common;

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
            state.Add<Data.ChallengeSpec>(fixture, spec =>
            {
                spec.Id = challengeSpecId;
                spec.Name = specName;
                spec.Game = state.Build<Data.Game>(fixture, g =>
                {
                    g.Id = fixture.Create<string>();
                    g.Players = state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = playerId;
                        p.User = state.Build<Data.User>(fixture, u => u.Id = userId);
                        p.SessionBegin = DateTimeOffset.UtcNow.AddDays(-1);
                        p.SessionEnd = DateTimeOffset.UtcNow.AddDays(1);
                    }).ToCollection();
                });
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
