using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api.Tests.Integration;

public interface ITestGameEngineStateChangeService
{
    public GameEngineGameState StartGamespaceResult { get; }
    public GameEngineGameState StopGamespaceResult { get; }
}

internal class TestGameEngineStateChangeService : ITestGameEngineStateChangeService
{
    public GameEngineGameState StartGamespaceResult { get; private set; }
    public GameEngineGameState StopGamespaceResult { get; private set; }

    public TestGameEngineStateChangeService
    (
        GameEngineGameState? startGamespaceResult = null,
        GameEngineGameState? stopGamespaceResult = null
    )
    {
        StartGamespaceResult = startGamespaceResult ?? new GameEngineGameState();
        StopGamespaceResult = stopGamespaceResult ?? new GameEngineGameState();
    }
}
