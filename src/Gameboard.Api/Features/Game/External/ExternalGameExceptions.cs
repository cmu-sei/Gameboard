using System.Collections.Generic;
using Gameboard.Api;
using Gameboard.Api.Structure;

public sealed class CantPreDeployNonExternalGame : GameboardValidationException
{
    public CantPreDeployNonExternalGame(string gameId)
        : base($"Can't predeploy resources for game {gameId} - it's not external/sync-start.") { }
}

public sealed class CantResolveTeamDeployStatus : GameboardException
{
    public CantResolveTeamDeployStatus(string gameId, string teamId)
        : base($"Couldn't resolve deploy status for team {teamId} in game {gameId}.") { }
}

public sealed class CantResolveGameDeployStatus : GameboardException
{
    public CantResolveGameDeployStatus(string gameId)
        : base($"Couldn't resolve deploy status for game {gameId}.") { }
}

internal class EmptyExternalHostUrl : GameboardException
{
    public EmptyExternalHostUrl(string gameId, string hostUrl)
        : base($"""Game ${gameId} doesn't have a configured external host URL (current value: "{hostUrl}".)""") { }
}

internal class EmptyExternalStartupEndpoint : GameboardException
{
    public EmptyExternalStartupEndpoint(string gameId, string startupUrl)
        : base($"""Game ${gameId} doesn't have a configured "startup" endpoint (current value: "{startupUrl}".)""") { }
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
