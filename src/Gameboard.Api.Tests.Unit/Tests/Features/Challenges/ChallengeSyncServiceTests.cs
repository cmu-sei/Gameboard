using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Services;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Tests.Unit;

public class ChallengeSyncServiceTests
{
    [Fact]
    public async Task GetExpiredChallengesForSync_WithPlayerWithNullishEndDate_DoesNotSync()
    {
        // given a player with a nullish end date
        var now = DateTimeOffset.UtcNow;
        var challenge = new Data.Challenge
        {
            EndTime = DateTimeOffset.MinValue,
            LastSyncTime = DateTimeOffset.MinValue,
            Player = new()
            {
                SessionEnd = DateTimeOffset.MinValue
            }
        };
        var store = BuildTestableStore(challenge);
        var sut = new ChallengeSyncService
        (
            A.Fake<ConsoleActorMap>(),
            A.Fake<IGameEngineService>(),
            A.Fake<ILogger<IChallengeSyncService>>(),
            A.Fake<IMapper>(),
            A.Fake<INowService>(),
            store
        );

        // when we query challenges to expire
        var challengesToExpire = await sut.GetExpiredChallengesForSync(now, CancellationToken.None);

        // we should get zero
        challengesToExpire.Count().ShouldBe(0);
    }

    [Fact]
    public async Task GetExpiredChallengesForSync_WithPlayerSessionEndInFuture_DoesNotSync()
    {
        // given a player with a nullish end date
        var now = DateTimeOffset.UtcNow;
        var challenge = new Data.Challenge
        {
            EndTime = DateTimeOffset.MinValue,
            LastSyncTime = DateTimeOffset.MinValue,
            Player = new()
            {
                SessionEnd = now.AddHours(1)
            }
        };
        var store = BuildTestableStore(challenge);
        var sut = new ChallengeSyncService
        (
            A.Fake<ConsoleActorMap>(),
            A.Fake<IGameEngineService>(),
            A.Fake<ILogger<IChallengeSyncService>>(),
            A.Fake<IMapper>(),
            A.Fake<INowService>(),
            store
        );

        // when we query challenges to expire
        var challengesToExpire = await sut.GetExpiredChallengesForSync(now, CancellationToken.None);

        // we should get zero
        challengesToExpire.Count().ShouldBe(0);
    }

    [Fact]
    public async Task GetExpiredChallengesForSync_WithChallengeAlreadySynced_DoesNotSync()
    {
        // given a player with a nullish end date
        var now = DateTimeOffset.UtcNow;
        var challenge = new Data.Challenge
        {
            EndTime = now.AddMinutes(-5),
            LastSyncTime = now.AddMinutes(-4),
            Player = new()
            {
                SessionEnd = now.AddHours(-1)
            }
        };
        var store = BuildTestableStore(challenge);
        var sut = new ChallengeSyncService
        (
            A.Fake<ConsoleActorMap>(),
            A.Fake<IGameEngineService>(),
            A.Fake<ILogger<IChallengeSyncService>>(),
            A.Fake<IMapper>(),
            A.Fake<INowService>(),
            store
        );

        // when we query challenges to expire
        var challengesToExpire = await sut.GetExpiredChallengesForSync(now, CancellationToken.None);

        // we should get zero
        challengesToExpire.Count().ShouldBe(0);
    }

    [Fact]
    public async Task GetExpiredChallengesForSync_WithNonNullishEndDate_DoesNotSync()
    {
        // given a player with a nullish end date
        var now = DateTimeOffset.UtcNow;
        var challenge = new Data.Challenge
        {
            EndTime = now.AddMinutes(-2),
            LastSyncTime = now.AddMinutes(-1),
            Player = new()
            {
                SessionEnd = now.AddMinutes(-2)
            }
        };
        var store = BuildTestableStore(challenge);
        var sut = new ChallengeSyncService
        (
            A.Fake<ConsoleActorMap>(),
            A.Fake<IGameEngineService>(),
            A.Fake<ILogger<IChallengeSyncService>>(),
            A.Fake<IMapper>(),
            A.Fake<INowService>(),
            store
        );

        // when we query challenges to expire
        var challengesToExpire = await sut.GetExpiredChallengesForSync(now, CancellationToken.None);

        // we should get zero
        challengesToExpire.Count().ShouldBe(0);
    }

    [Fact]
    public async Task GetExpiredChallengesForSync_WithAllRequiredCriteriaAndNullishEndDate_Syncs()
    {
        // given a player with a nullish end date
        var now = DateTimeOffset.UtcNow;
        var challenge = new Data.Challenge
        {
            EndTime = DateTimeOffset.MinValue,
            LastSyncTime = now.AddMinutes(-3),
            Player = new()
            {
                SessionEnd = now.AddMinutes(-2)
            }
        };
        var store = BuildTestableStore(challenge);
        var sut = new ChallengeSyncService
        (
            A.Fake<ConsoleActorMap>(),
            A.Fake<IGameEngineService>(),
            A.Fake<ILogger<IChallengeSyncService>>(),
            A.Fake<IMapper>(),
            A.Fake<INowService>(),
            store
        );

        // when we query challenges to expire
        var challengesToExpire = await sut.GetExpiredChallengesForSync(now, CancellationToken.None);

        // we should get zero
        challengesToExpire.Count().ShouldBe(1);
    }

    private IStore BuildTestableStore(params Data.Challenge[] challenges)
    {
        var store = A.Fake<IStore>();
        A
            .CallTo(() => store.WithNoTracking<Data.Challenge>())
            .Returns(challenges.BuildMock());

        return store;
    }
}
