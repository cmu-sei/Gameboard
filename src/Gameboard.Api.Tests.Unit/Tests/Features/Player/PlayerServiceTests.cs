using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Gameboard.Api.Tests.Unit;

public class PlayerServiceTests
{
    class PlayerServiceTestable : PlayerService
    {
        private PlayerServiceTestable(
            CoreOptions coreOptions,
            ChallengeService challengeService,
            IPlayerStore store,
            IGameHubBus gameHubBus,
            IGameService gameService,
            IGameStore gameStore,
            IGuidService guidService,
            IMediator mediator,
            INowService now,
            IInternalHubBus hubBus,
            ITeamService teamService,
            IMapper mapper,
            IMemoryCache localCache,
            GameEngineService gameEngine,
            IStore<Data.User> userStore) : base
            (
                coreOptions,
                challengeService,
                guidService,
                mediator,
                now,
                store,
                gameHubBus,
                gameService,
                gameStore,
                hubBus,
                teamService,
                mapper,
                localCache,
                gameEngine,
                userStore
            )
        {
        }

        // TODO: reflection helper
        internal static PlayerService GetTestable(
            CoreOptions? coreOptions = null,
            ChallengeService? challengeService = null,
            IPlayerStore? store = null,
            IGameHubBus? gameHubBus = null,
            IGameService? gameService = null,
            IGameStore? gameStore = null,
            IGuidService? guidService = null,
            IMediator? mediator = null,
            INowService? now = null,
            IInternalHubBus? hubBus = null,
            ITeamService? teamService = null,
            IMapper? mapper = null,
            IMemoryCache? localCache = null,
            GameEngineService? gameEngine = null,
            IStore<Data.User>? userStore = null)
        {
            return new PlayerService
            (
                coreOptions ?? A.Fake<CoreOptions>(),
                challengeService ?? A.Fake<ChallengeService>(),
                guidService ?? A.Fake<IGuidService>(),
                mediator ?? A.Fake<IMediator>(),
                now ?? A.Fake<INowService>(),
                store ?? A.Fake<IPlayerStore>(),
                gameHubBus ?? A.Fake<IGameHubBus>(),
                gameService ?? A.Fake<GameService>(),
                gameStore ?? A.Fake<IGameStore>(),
                hubBus ?? A.Fake<IInternalHubBus>(),
                teamService ?? A.Fake<ITeamService>(),
                mapper ?? A.Fake<IMapper>(),
                localCache ?? A.Fake<IMemoryCache>(),
                gameEngine ?? A.Fake<GameEngineService>(),
                userStore ?? A.Fake<IStore<Data.User>>()
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
