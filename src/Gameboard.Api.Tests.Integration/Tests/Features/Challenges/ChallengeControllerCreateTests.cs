using Gameboard.Api.Common;

namespace Gameboard.Api.Tests.Integration;

public class ChallengeControllerCreateTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public ChallengeControllerCreateTests(GameboardTestContext testContext)
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
                        p.Role = PlayerRole.Manager;
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

        // act
        var challenge = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PostAsync("/api/challenge/launch", model.ToJsonBody())
            .DeserializeResponseAs<Api.Challenge>();

        // assert
        challenge?.Name.ShouldBe(specName);
        challenge?.State.ManagerId.ShouldBe(playerId);
    }
}
