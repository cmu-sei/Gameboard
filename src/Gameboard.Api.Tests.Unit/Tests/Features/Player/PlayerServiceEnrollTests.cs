using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Player;

namespace Gameboard.Api.Tests.Unit;

public class PlayerServiceEnrollTests
{
    [Theory, GameboardAutoData]
    public async Task Enroll_WithNoSponsor_ThrowsExpected(string gameId, string userId, IFixture fixture)
    {
        // given
        var store = A.Fake<IStore>();
        A
            .CallTo(() => store.WithNoTracking<Data.Game>())
            .WithAnyArguments()
            .Returns(new Data.Game
            {
                Id = gameId,
                RegistrationType = GameRegistrationType.Open,
                RegistrationOpen = DateTimeOffset.UtcNow.AddDays(-1),
                RegistrationClose = DateTimeOffset.UtcNow.AddDays(1)
            }.ToCollection().BuildMock());

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


        var sut = PlayerServiceTestHelpers.GetTestableSut(store: store);

        var request = new NewPlayer
        {
            GameId = gameId,
            UserId = userId,
        };

        var actor = new User
        {
            Id = userId,
            Role = UserRole.Member,
            RolePermissions = [],
        };

        // when/then
        await Should.ThrowAsync<NoPlayerSponsorForGame>(() => sut.Enroll(request, actor, CancellationToken.None));
    }

    [Theory, GameboardAutoData]
    public async Task Enroll_WithExistingCompetitiveModeRegistration_ThrowsExpected(string gameId, string userId, IFixture fixture)
    {
        // given
        var game = new Data.Game
        {
            Id = gameId,
            PlayerMode = PlayerMode.Competition,
            GameStart = DateTimeOffset.UtcNow.AddDays(-1),
            GameEnd = DateTimeOffset.UtcNow.AddDays(1),
            RegistrationType = GameRegistrationType.Open,
            RegistrationOpen = DateTimeOffset.UtcNow.AddDays(-1),
            RegistrationClose = DateTimeOffset.UtcNow.AddDays(1)
        };

        var store = A.Fake<IStore>();
        A
            .CallTo(() => store.WithNoTracking<Data.User>())
            .WithAnyArguments()
            .Returns(new Data.User
            {
                Id = userId,
                SponsorId = fixture.Create<string>(),
                Enrollments = new Data.Player()
                {
                    Id = fixture.Create<string>(),
                    GameId = gameId,
                    Mode = PlayerMode.Competition,
                    SponsorId = fixture.Create<string>(),
                }.ToCollection()
            }.ToCollection().BuildMock());

        A
            .CallTo(() => store.WithNoTracking<Data.Game>())
            .WithAnyArguments()
            .Returns(new List<Data.Game>([game]).BuildMock());

        var sut = PlayerServiceTestHelpers.GetTestableSut(store: store);

        var request = new NewPlayer
        {
            GameId = gameId,
            UserId = userId,
        };

        var actor = new User
        {
            Id = userId,
            Role = UserRole.Member,
            RolePermissions = []
        };

        // when/then
        await Should.ThrowAsync<AlreadyRegistered>(() => sut.Enroll(request, actor, CancellationToken.None));
    }
}
