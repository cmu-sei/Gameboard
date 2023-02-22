using Gameboard.Api;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Validators;

namespace Gameboard.Tests.Unit;

public class PlayerValidatorTests
{
    private IPlayerStore _BuildPlayerStore(IFixture fixture, string actingPlayerId, params Api.Data.Player[] players)
    {
        var actingPlayer = players.Single(p => p.Id == actingPlayerId);
        var mockPlayers = players.BuildMock();
        var playerStore = A.Fake<IPlayerStore>();

        A.CallTo(() => playerStore.Retrieve(actingPlayerId)).Returns(actingPlayer);
        A.CallTo(() => playerStore.List(null)).Returns(mockPlayers);

        return playerStore;
    }

    [Theory, GameboardAutoData]
    public void ValidateUnenroll_WhenIsManagerAndHasTeammates_Fails(IFixture fixture)
    {
        // arrange
        var teamId = fixture.Create<string>();

        var managerPlayer = new Api.Data.Player
        {
            Id = "manager",
            Role = PlayerRole.Manager,
            TeamId = teamId,
            UserId = fixture.Create<string>()
        };
        var nonManagerPlayer = new Api.Data.Player
        {
            Id = "member",
            Role = PlayerRole.Member,
            TeamId = teamId
        };

        var sut = new PlayerValidator(_BuildPlayerStore(fixture, "manager", new Api.Data.Player[]
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
        var player = new Api.Data.Player
        {
            Id = "manager",
            Role = PlayerRole.Manager,
            TeamId = fixture.Create<string>(),
            UserId = fixture.Create<string>()
        };

        var playerStore = _BuildPlayerStore(fixture, player.Id, player);
        var sut = new PlayerValidator(playerStore);

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

        var player = new Api.Data.Player
        {
            Id = "member",
            Role = PlayerRole.Member,
            // this is the key part - this player has started the session
            SessionBegin = fixture.Create<DateTimeOffset>(),
            TeamId = teamId
        };

        var playerStore = _BuildPlayerStore(fixture, player.Id, player);
        var sut = new PlayerValidator(playerStore);

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

        var player = new Api.Data.Player
        {
            Id = "member",
            Role = PlayerRole.Member,
            // this is the key part - this player has started the session
            SessionBegin = fixture.Create<DateTimeOffset>(),
            TeamId = teamId
        };

        var playerStore = _BuildPlayerStore(fixture, player.Id, player);
        var sut = new PlayerValidator(playerStore);

        // act / assert
        await Should.NotThrowAsync(() => sut._validate(new PlayerUnenrollRequest
        {
            Actor = new User { Id = player.UserId, Role = UserRole.Admin },
            PlayerId = player.Id,
            AsAdmin = true
        }));
    }
}
