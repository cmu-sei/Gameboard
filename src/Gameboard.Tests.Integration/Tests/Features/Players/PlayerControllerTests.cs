using Gameboard.Api;
using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration;

public class PlayerControllerTests : IClassFixture<GameboardTestContext<GameboardDbContextPostgreSQL>>
{
    private readonly GameboardTestContext<GameboardDbContextPostgreSQL> _testContext;

    public PlayerControllerTests(GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        _testContext = testContext;
    }

    [Fact]
    public async Task Update_WhenNameNotUniqueInGame_SetsNameNotUnique()
    {
        // given
        var game = await _testContext.GetDbContext().CreateGame(GameBuilder.WithConfig(g => g.Id = "same game"));
        var playerA = await _testContext.GetDbContext().CreatePlayer
        (
            p =>
            {
                p.Name = "A";
                p.GameId = game.Id;
                p.TeamId = "team A";
            }
        );

        var playerB = await _testContext.GetDbContext().CreatePlayer
        (
            p =>
            {
                p.Name = "B";
                p.GameId = game.Id;
                p.TeamId = "team B";
            }
        );

        var httpClient = _testContext.WithAuthentication().CreateClient();
        var sutParams = new ChangedPlayer
        {
            Id = playerB.Id,
            // tries to update `playerB` to have the same name as `playerA`
            Name = playerA.Name,
            ApprovedName = playerB.ApprovedName,
            Sponsor = "sponsor",
            Role = PlayerRole.Member
        };

        // when
        var updatedPlayer = await httpClient
            .PutAsync("/api/player", sutParams.ToJsonBody())
            .WithContentDeserializedAs<Api.Player>(_testContext.GetJsonSerializerOptions());

        // assert
        updatedPlayer.NameStatus.ShouldBe(AppConstants.NameStatusNotUnique);
    }
}