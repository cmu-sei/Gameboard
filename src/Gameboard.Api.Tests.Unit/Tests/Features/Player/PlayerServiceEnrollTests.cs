using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Player;

namespace Gameboard.Api.Tests.Unit;

public class PlayerServiceEnrollTests
{
    [Theory, GameboardAutoData]
    public async Task Enroll_WithNoSponsor_ThrowsExpected(string gameId, string userId, IFixture fixture)
    {
        // given
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

        var store = A.Fake<IStore>();
        A
            .CallTo(() => store.WithNoTracking<Data.User>())
            .WithAnyArguments()
            .Returns(new Data.User
            {
                Id = userId,
                Enrollments = new Data.Player()
                {
                    Id = fixture.Create<string>()
                }.ToCollection()
            }.ToCollection().BuildMock());


        var sut = PlayerServiceTestHelpers.GetTestableSut
        (
            gameStore: gameStore,
            store: store
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
