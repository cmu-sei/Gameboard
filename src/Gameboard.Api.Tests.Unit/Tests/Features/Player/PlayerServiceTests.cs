using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Teams;
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
            IUserStore userStore,
            IGameService gameService,
            IGameStore gameStore,
            IGuidService guidService,
            IMediator mediator,
            IInternalHubBus hubBus,
            INowService now,
            IGameStartService gameStartService,
            ITeamService teamService,
            IMapper mapper,
            IMemoryCache localCache,
            GameEngineService gameEngine) : base
            (
                coreOptions,
                challengeService,
                gameStartService,
                guidService,
                mediator,
                store,
                userStore,
                gameService,
                gameStore,
                hubBus,
                now,
                teamService,
                mapper,
                localCache,
                gameEngine
            )
        {
        }

        // TODO: reflection helper
        internal static PlayerService GetTestable(
            CoreOptions? coreOptions = null,
            ChallengeService? challengeService = null,
            IPlayerStore? store = null,
            IUserStore? userStore = null,
            IGameService? gameService = null,
            IGameStartService? gameStartService = null,
            IGameStore? gameStore = null,
            IGuidService? guidService = null,
            IMediator? mediator = null,
            IInternalHubBus? hubBus = null,
            ITeamService? teamService = null,
            IMapper? mapper = null,
            IMemoryCache? localCache = null,
            GameEngineService? gameEngine = null,
            INowService? now = null)
        {
            return new PlayerService
            (
                coreOptions ?? A.Fake<CoreOptions>(),
                challengeService ?? A.Fake<ChallengeService>(),
                gameStartService ?? A.Fake<IGameStartService>(),
                guidService ?? A.Fake<IGuidService>(),
                mediator ?? A.Fake<IMediator>(),
                store ?? A.Fake<IPlayerStore>(),
                userStore ?? A.Fake<IUserStore>(),
                gameService ?? A.Fake<GameService>(),
                gameStore ?? A.Fake<IGameStore>(),
                hubBus ?? A.Fake<IInternalHubBus>(),
                now ?? A.Fake<INowService>(),
                teamService ?? A.Fake<ITeamService>(),
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
