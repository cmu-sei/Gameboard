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
            Game = new SimpleEntity { Id = gameId, Name = fixture.Create<string>() },
            Context = new GameStartContext
            {
                Game = new SimpleEntity { Id = gameId, Name = "game" },
                SessionLengthMinutes = fixture.Create<int>(),
                SpecIds = fixture.CreateMany<string>(challengeCount),
                TotalChallengeCount = challengeCount,
                TotalGamespaceCount = challengeCount
            }
        };

        request.Context.ChallengesCreated.AddRange(fixture.CreateMany<GameStartContextChallenge>(challengeCount).ToList());
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
            A.Fake<ILogger<ExternalGameDeployBatchService>>()
        );

        // when batches are built
        var result = sut.BuildDeployBatches(fixture.CreateMany<GameStartContextChallenge>(challengeCount));

        // we expect three batches
        result.Count().ShouldBe(3);
        // and the last should have 5 tasks in it
        result.Last().Count().ShouldBe(5);
    }

    [Theory, GameboardAutoData]
    public void BuildDeployBatches_WithNoConfiguredBatchSize_ReturnsExpectedBatchCount(int challengeCount, IFixture fixture)
    {
        // given a deploy request with any challenge count and no set batch size
        var challenges = fixture.CreateMany<GameStartContextChallenge>(challengeCount);
        var request = BuildFakeGameModeStartRequest(challengeCount, fixture);

        var sut = new ExternalGameDeployBatchService
        (
            new CoreOptions { GameEngineDeployBatchSize = 0 },
            A.Fake<ILogger<ExternalGameDeployBatchService>>()
        );

        // when batches are built
        var result = sut.BuildDeployBatches(challenges);

        // we expect one batch with length equal to the challenge count
        result.Count().ShouldBe(challengeCount);
        result.First().Count().ShouldBe(1);
    }
}
