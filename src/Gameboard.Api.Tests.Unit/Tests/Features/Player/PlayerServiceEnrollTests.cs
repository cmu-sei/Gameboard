using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Player;

namespace Gameboard.Api.Tests.Unit;

public class PlayerServiceEnrollTests
{
    [Theory, GameboardAutoData]
    public async Task Enroll_WithNoSponsor_ThrowsExpected(string gameId, string userId)
    {
        // arrange
        var gameStore = A.Fake<IGameStore>();
        A
            .CallTo(() => gameStore.Retrieve(gameId))
            .WithAnyArguments()
            .Returns(new Data.Game
            {
                Id = gameId,
                RegistrationType = GameRegistrationType.Open,
                RegistrationOpen = DateTimeOffset.UtcNow.AddDays(-1),
                RegistrationClose = DateTimeOffset.UtcNow.AddDays(1)
            });

        var playerStore = A.Fake<IPlayerStore>();
        A
            .CallTo(() => playerStore.GetUserEnrollments(userId))
            .WithAnyArguments()
            .Returns(new Data.User { Id = userId, });

        var sut = PlayerServiceTestHelpers.GetTestableSut
        (
            gameStore: gameStore,
            playerStore: playerStore
        );

        var request = new NewPlayer
        {
            GameId = gameId,
            UserId = userId,
        };

        var actor = new Api.User
        {
            Id = userId,
            Role = UserRole.Member,
        };

        // when/then
        await Should.ThrowAsync<NoPlayerSponsorForGame>(() => sut.Enroll(request, actor, CancellationToken.None));
    }
}
