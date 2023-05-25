using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public interface ITestGradingResultService
{
    GameEngineGameState Get();
}

internal class TestGradingResultService : ITestGradingResultService
{
    private readonly Func<GameEngineGameState> _gameStateBuilder;

    public TestGradingResultService(Func<GameEngineGameState> gameStateBuilder)
    {
        _gameStateBuilder = gameStateBuilder;
    }

    public GameEngineGameState Get()
    {
        return _gameStateBuilder.Invoke();
    }
}
