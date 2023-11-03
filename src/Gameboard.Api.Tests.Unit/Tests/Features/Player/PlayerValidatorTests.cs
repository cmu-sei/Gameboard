using Gameboard.Api.Data;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Validators;

namespace Gameboard.Api.Tests.Unit;

public class PlayerValidatorTests
{
    private IStore BuildStoreWithActingPlayer(string actingPlayerId, params Data.Player[] players)
    {
        var actingPlayer = players.Single(p => p.Id == actingPlayerId);
        var mockPlayers = players.BuildMock();

        var store = A.Fake<IStore>();
        A.CallTo(() => store.WithNoTracking<Data.Player>()).Returns(mockPlayers);

        return store;
    }

    [Theory, GameboardAutoData]
    public void ValidateUnenroll_WhenIsManagerAndHasTeammates_Fails(IFixture fixture)
    {
        // arrange
        var teamId = fixture.Create<string>();

        var managerPlayer = new Data.Player
        {
            Id = "manager",
            Role = PlayerRole.Manager,
            TeamId = teamId,
            UserId = fixture.Create<string>()
        };
        var nonManagerPlayer = new Data.Player
        {
            Id = "member",
            Role = PlayerRole.Member,
            TeamId = teamId
        };

        var sut = new PlayerValidator(A.Fake<IPlayerStore>(), BuildStoreWithActingPlayer("manager", new Data.Player[]
        {
            managerPlayer, nonManagerPlayer
        }));

        // act / assert
        Should.Throw<ManagerCantUnenrollWhileTeammatesRemain>(async () => await sut._validate(new PlayerUnenrollRequest
        {
            Actor = new User { Id = fixture.Create<string>() },
            PlayerId = managerPlayer.Id
        }));
    }

    [Theory, GameboardAutoData]
    public async Task ValidateUnenroll_WhenIsManagerAndHasNoTeammates_Succeeds(IFixture fixture)
    {
        // arrange
        var player = new Data.Player
        {
            Id = "manager",
            Role = PlayerRole.Manager,
            TeamId = fixture.Create<string>(),
            UserId = fixture.Create<string>()
        };

        var store = BuildStoreWithActingPlayer(player.Id, player);
        var sut = new PlayerValidator(A.Fake<IPlayerStore>(), store);

        // act / assert
        await Should.NotThrowAsync(() => sut._validate(new PlayerUnenrollRequest
        {
            Actor = new User { Id = player.UserId },
            PlayerId = player.Id
        }));
    }

    [Theory, GameboardAutoData]
    public void ValidateUnenroll_WhenSessionStartedAndNotAsAdmin_Fails(IFixture fixture)
    {
        // arrange
        var sessionStart = fixture.Create<DateTimeOffset>();
        var teamId = fixture.Create<string>();

        var player = new Data.Player
        {
            Id = "member",
            Role = PlayerRole.Member,
            // this is the key part - this player has started the session
            SessionBegin = fixture.Create<DateTimeOffset>(),
            TeamId = teamId
        };

        var store = BuildStoreWithActingPlayer(player.Id, player);
        var sut = new PlayerValidator(A.Fake<IPlayerStore>(), store);

        // act / assert
        Should.Throw<SessionAlreadyStarted>(async () => await sut._validate(new PlayerUnenrollRequest
        {
            Actor = new User { Id = player.UserId },
            PlayerId = player.Id
        }));
    }

    [Theory, GameboardAutoData]
    public async Task ValidateUnenroll_WhenSessionStartedAndAsAdmin_Passes(IFixture fixture)
    {
        // arrange
        var sessionStart = fixture.Create<DateTimeOffset>();
        var teamId = fixture.Create<string>();

        var player = new Data.Player
        {
            Id = "member",
            Role = PlayerRole.Member,
            // this is the key part - this player has started the session
            SessionBegin = fixture.Create<DateTimeOffset>(),
            TeamId = teamId
        };

        var store = BuildStoreWithActingPlayer(player.Id, player);
        var sut = new PlayerValidator(A.Fake<IPlayerStore>(), store);

        // act / assert
        await Should.NotThrowAsync(() => sut._validate(new PlayerUnenrollRequest
        {
            Actor = new User { Id = player.UserId, Role = UserRole.Admin },
            PlayerId = player.Id,
            AsAdmin = true
        }));
    }
}
