using Gameboard.Api.Common;

namespace Gameboard.Api.Tests.Integration;

public class PlayerControllerEnrollTests : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext;

    public PlayerControllerEnrollTests(GameboardTestContext testContext)
    {
        _testContext = testContext;
    }

    [Theory, GbIntegrationAutoData]
    public async Task Enroll_WithPriorPracticeModeRegistration_DoesntThrow(string gameId, string userId, IFixture fixture)
    {
        // given
        await _testContext.WithDataState(state =>
        {
            state.Add<Data.Game>(fixture, game =>
            {
                game.Id = gameId;
                game.PlayerMode = PlayerMode.Competition;
            });

            state.Add<Data.User>(fixture, u =>
            {
                u.Id = userId;
                u.Role = UserRole.Member;
                u.Sponsor = fixture.Create<Data.Sponsor>();
                u.Enrollments = state.Build<Data.Player>(fixture, p =>
                {
                    p.Id = fixture.Create<string>();
                    p.Mode = PlayerMode.Practice;
                    p.GameId = gameId;
                }).ToCollection();
            });
        });

        var enrollRequest = new NewPlayer()
        {
            UserId = userId,
            GameId = gameId,
            Name = fixture.Create<string>()
        };

        var httpClient = _testContext.CreateHttpClientWithActingUser(u => u.Id = userId); ;

        // when
        var result = await httpClient
            .PostAsync("/api/player", enrollRequest.ToJsonBody())
            .WithContentDeserializedAs<Player>();

        result.ShouldNotBeNull();
        result.UserId.ShouldBe(userId);
        result.GameId.ShouldBe(gameId);
    }
}
