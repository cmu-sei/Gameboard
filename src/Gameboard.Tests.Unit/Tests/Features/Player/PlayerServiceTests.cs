using AutoMapper;
using Gameboard.Api;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using TopoMojo.Api.Client;

namespace Gameboard.Tests.Unit;

public class PlayerServiceTests
{
    class PlayerServiceTestable : PlayerService
    {
        private PlayerServiceTestable(CoreOptions coreOptions, IPlayerStore store, IUserStore userStore, IGameStore gameStore, IMapper mapper, IMemoryCache localCache, GameEngineService gameEngine) : base(coreOptions, store, userStore, gameStore, mapper, localCache, gameEngine)
        {
        }

        // TODO: reflection helper
        internal static PlayerService GetTestable(CoreOptions? coreOptions = null, IPlayerStore? store = null, IUserStore? userStore = null, IGameStore? gameStore = null, IMapper? mapper = null, IMemoryCache? localCache = null, GameEngineService? gameEngine = null)
        {
            return new PlayerService
            (
                coreOptions ?? A.Fake<CoreOptions>(),
                store ?? A.Fake<IPlayerStore>(),
                userStore ?? A.Fake<IUserStore>(),
                gameStore ?? A.Fake<IGameStore>(),
                mapper ?? A.Fake<IMapper>(),
                localCache ?? A.Fake<IMemoryCache>(),
                gameEngine ?? A.Fake<GameEngineService>()
            );
        }
    }

    [Theory, GameboardAutoData]
    public async Task Standings_WhenGameIdIsEmpty_ReturnsEmptyArray(IFixture fixture)
    {
        // arrange
        var sut = fixture.Create<PlayerService>();
        var filterParams = A.Fake<PlayerDataFilter>();

        // act
        var result = await sut.Standings(filterParams);

        // assert
        result.ShouldBe(new Standing[] { });
    }

    [Theory, GameboardAutoData]
    public async Task MakeCertificates_WhenScoreZero_ReturnsEmptyArray(IFixture fixture)
    {
        // arrange
        var userId = fixture.Create<string>();
        var fakeStore = A.Fake<IPlayerStore>();
        var fakePlayers = new Api.Data.Player[]
        {
            new Api.Data.Player
            {
                PartialCount = 1,
                Game = new Api.Data.Game
                {
                    CertificateTemplate = fixture.Create<string>(),
                    GameEnd = DateTimeOffset.Now - TimeSpan.FromDays(1)
                },
                Score = 0,
                SessionEnd = DateTimeOffset.Now - TimeSpan.FromDays(2),
                User = new Api.Data.User { Id = userId }
            }
        }.ToList().BuildMock();

        A.CallTo(() => fakeStore.List(null)).Returns(fakePlayers);
        A.CallTo(() => fakeStore.DbSet).Returns(fakePlayers);

        var sut = PlayerServiceTestable.GetTestable(store: fakeStore);

        // act
        var result = await sut.MakeCertificates(userId);

        // assert
        result.ShouldBe(new PlayerCertificate[] { });
    }

    [Theory, GameboardAutoData]
    public async Task MakeCertificates_WhenScore1_ReturnsOneCertificate(IFixture fixture)
    {
        // arrange
        var userId = fixture.Create<string>();
        var fakeStore = A.Fake<IPlayerStore>();
        var fakePlayers = new Api.Data.Player[]
        {
            new Api.Data.Player
            {
                PartialCount = 0,
                Game = new Api.Data.Game
                {
                    CertificateTemplate = fixture.Create<string>(),
                    GameEnd = DateTimeOffset.Now - TimeSpan.FromDays(1)
                },
                Score = 1,
                SessionEnd = DateTimeOffset.Now - TimeSpan.FromDays(2),
                UserId = userId,
                User = new Api.Data.User { Id = userId }
            }
        }.ToList().BuildMock();

        A.CallTo(() => fakeStore.List(null)).Returns(fakePlayers);
        A.CallTo(() => fakeStore.DbSet).Returns(fakePlayers);

        var sut = PlayerServiceTestable.GetTestable(store: fakeStore);

        // act
        var result = await sut.MakeCertificates(userId);

        // assert
        result.Count().ShouldBe(1);
    }
}
