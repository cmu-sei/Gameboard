using Gameboard.Api.Common;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Tests.Integration.Teams;

public class TeamControllerStartTeamSessionTests(GameboardTestContext testContext) : IClassFixture<GameboardTestContext>
{
    private readonly GameboardTestContext _testContext = testContext;

    [Theory, GbIntegrationAutoData]
    public async Task TeamGame_WithSinglePlayer_CantStart
    (
        string gameId,
        string playerId,
        string userId,
        IFixture fixture
    )
    {
        // given a team game and a registered player with no teammates
        await _testContext.WithDataState(state =>
        {
            state.Add(new Data.Game
            {
                Id = gameId,
                MinTeamSize = 2,
                MaxTeamSize = 5,
                GameStart = DateTimeOffset.UtcNow,
                GameEnd = DateTimeOffset.UtcNow.AddDays(1),
                Mode = GameEngineMode.Standard,
                Players = state.Build<Data.Player>(fixture, p =>
                {
                    p.Id = playerId;
                    p.User = state.Build<Data.User>(fixture, u => u.Id = userId);
                }).ToCollection()
            });
        });

        // when they try to start their session
        await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PutAsync($"api/player/{playerId}/start", null)
            // they should get a validation error
            .ShouldYieldGameboardValidationException<GameboardAggregatedValidationExceptions>();
    }

    [Theory, GbIntegrationAutoData]
    public async Task TeamGame_WithTwoPlayers_CanStart
    (
        string gameId,
        string playerId,
        string userId,
        string teamId,
        IFixture fixture
    )
    {
        // given a team game and a registered player with no teammates
        await _testContext.WithDataState(state =>
        {
            state.Add(new Data.Game
            {
                Id = gameId,
                MinTeamSize = 2,
                MaxTeamSize = 5,
                GameStart = DateTimeOffset.UtcNow,
                GameEnd = DateTimeOffset.UtcNow.AddDays(1),
                Mode = GameEngineMode.Standard,
                Players =
                [
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = playerId;
                        p.Role = PlayerRole.Manager;
                        p.TeamId = teamId;
                        p.User = state.Build<Data.User>(fixture, u => u.Id = userId);
                    }),
                    state.Build<Data.Player>(fixture, p =>
                    {
                        p.Id = fixture.Create<string>();
                        p.Role = PlayerRole.Member;
                        p.TeamId = teamId;
                        p.User = state.Build<Data.User>(fixture, u => u.Id = fixture.Create<string>());
                    })
                ]
            });
        });

        // when they try to start their session
        var result = await _testContext
            .CreateHttpClientWithActingUser(u => u.Id = userId)
            .PutAsync($"api/player/{playerId}/start", null)
            .DeserializeResponseAs<Player>();

        // then we should get a player back with a nonempty session start
        result.SessionBegin.ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }
}
