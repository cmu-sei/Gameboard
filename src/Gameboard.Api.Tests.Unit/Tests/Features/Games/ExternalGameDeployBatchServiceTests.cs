using Gameboard.Api.Common;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.External;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Tests.Unit;

public class ExternalGameDeployBatchServiceTests
{
    private GameModeStartRequest BuildFakeGameModeStartRequest(int challengeCount, IFixture fixture)
    {
        var gameId = fixture.Create<string>();

        var request = new GameModeStartRequest
        {
            GameId = fixture.Create<string>(),
            State = new GameStartState
            {
                Game = new SimpleEntity { Id = gameId, Name = "game" },
                ChallengesTotal = challengeCount,
                Now = DateTimeOffset.UtcNow
            },
            Context = new GameModeStartRequestContext
            {
                SessionLengthMinutes = fixture.Create<int>(),
                SpecIds = fixture.CreateMany<string>(challengeCount)
            }
        };

        request.State.ChallengesCreated.AddRange(fixture.CreateMany<GameStartStateChallenge>(challengeCount).ToList());
        return request;
    }

    [Theory, GameboardAutoData]
    public void BuildDeployBatches_WithFixedBatchSizeAndChallengeCount_ReturnsCorrectBatchCount(IFixture fixture)
    {
        // given a deploy request with 17 challenges and a batch size of 6
        var challengeCount = 17;
        var request = BuildFakeGameModeStartRequest(challengeCount, fixture);

        // create sut and its options
        var sut = new ExternalGameDeployBatchService
        (
            new CoreOptions { GameEngineDeployBatchSize = 6 },
            A.Fake<IGameEngineService>(),
            A.Fake<IGameHubBus>(),
            A.Fake<ILogger<ExternalGameDeployBatchService>>()
        );

        // when batches are built
        var result = sut.BuildDeployBatches(request);

        // we expect three batches
        result.Count().ShouldBe(3);
        // and the last should have 5 tasks in it
        result.Last().Count().ShouldBe(5);
    }

    [Theory, GameboardAutoData]
    public void BuildDeployBatches_WithNoConfiguredBatchSize_ReturnsExpectedBatchCount(IFixture fixture)
    {
        // given a deploy request with any challenge count and no set batch size
        var challengeCount = fixture.Create<int>();
        var request = BuildFakeGameModeStartRequest(challengeCount, fixture);

        var sut = new ExternalGameDeployBatchService
        (
            new CoreOptions { GameEngineDeployBatchSize = 0 },
            A.Fake<IGameEngineService>(),
            A.Fake<IGameHubBus>(),
            A.Fake<ILogger<ExternalGameDeployBatchService>>()
        );

        // when batches are built
        var result = sut.BuildDeployBatches(request);

        // we expect one batch with length equal to the challenge count
        result.Count().ShouldBe(challengeCount);
        result.First().Count().ShouldBe(1);
    }
}
