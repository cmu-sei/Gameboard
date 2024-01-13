using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public sealed class TestGradingResultServiceConfiguration
{
    public Action<GameEngineGameState>? GameStateBuilder { get; set; }
    public Exception? ThrowsOnGrading { get; set; }
}

public interface ITestGradingResultService
{
    GameEngineGameState Get(Data.Challenge challenge);
}

internal class TestGradingResultService : ITestGradingResultService
{
    private readonly TestGradingResultServiceConfiguration _config;

    public TestGradingResultService(TestGradingResultServiceConfiguration config)
    {
        _config = config;
    }

    public GameEngineGameState Get(Data.Challenge challenge)
    {
        if (_config.ThrowsOnGrading is not null)
            throw _config.ThrowsOnGrading;

        var state = BuildChallengeStateFromChallenge(challenge);
        _config?.GameStateBuilder?.Invoke(state);
        return state;
    }

    private GameEngineGameState BuildChallengeStateFromChallenge(Data.Challenge challenge)
        => challenge == null ? new GameEngineGameState() : new GameEngineGameState
        {
            Id = challenge.Id,
            Name = challenge.Name,
            ManagerId = challenge.PlayerId,
            ManagerName = challenge.Player?.Name,
            IsActive = true,
            Players = new List<GameEnginePlayer>
            {
                new()
                {
                    GamespaceId = challenge.Id,
                    SubjectId = challenge.PlayerId,
                    SubjectName = challenge.Player?.ApprovedName,
                    Permission = GameEnginePlayerPermission.Manager,
                    IsManager = true
                }
            },
            WhenCreated = challenge.StartTime,
            Challenge = new GameEngineChallengeView
            {

                MaxPoints = challenge.Points,
                Score = challenge.Score
            }
        };
}
