using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public interface ITestGradingResultService
{
    GameEngineGameState Get(Data.Challenge challenge);
}

internal class TestGradingResultService : ITestGradingResultService
{
    private readonly Action<GameEngineGameState> _gameStateBuilder;

    public TestGradingResultService(Action<GameEngineGameState> gameStateBuilder)
    {
        _gameStateBuilder = gameStateBuilder;
    }

    public GameEngineGameState Get(Data.Challenge challenge)
    {
        var state = BuildChallengeStateFromChallenge(challenge);
        _gameStateBuilder.Invoke(state);
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
            Players = new GameEnginePlayer[]
            {
                new GameEnginePlayer
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