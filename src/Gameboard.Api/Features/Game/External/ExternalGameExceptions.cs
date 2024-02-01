using System.Collections.Generic;
using Gameboard.Api.Structure;

public sealed class CantPreDeployNonExternalGame : GameboardValidationException
{
    public CantPreDeployNonExternalGame(string gameId)
        : base($"Can't predeploy resources for game {gameId} - it's not external/sync-start.") { }
}

public sealed class GameHasUnexpectedEngineMode : GameboardValidationException
{
    public GameHasUnexpectedEngineMode(string gameId, string engineMode, string expectedEngineMode)
        : base($"Game {gameId} had an unexpected engine mode. Expected: {expectedEngineMode} (Actual: {engineMode})") { }
}

public sealed class GameHasUnexpectedSyncStart : GameboardValidationException
{
    public GameHasUnexpectedSyncStart(string gameId, bool expectsSyncStart)
        : base($"Game {gameId} has unexpected sync start setting. Expected: {expectsSyncStart} (Actual: {!expectsSyncStart})") { }
}

public sealed class GameResourcesArentDeployedOnStart : GameboardException
{
    public GameResourcesArentDeployedOnStart(string gameId, IEnumerable<string> undeployedGamespaceIds)
        : base($"Couldn't launch game {gameId}: some of its gamespaces aren't deployed ({string.Join(", ", undeployedGamespaceIds)}).") { }
}
