using Gameboard.Api.Structure;

public sealed class CantPreDeployNonExternalGame : GameboardValidationException
{
    public CantPreDeployNonExternalGame(string gameId)
        : base($"Can't predeploy resources for game {gameId} - it's not external/sync-start.") { }
}
