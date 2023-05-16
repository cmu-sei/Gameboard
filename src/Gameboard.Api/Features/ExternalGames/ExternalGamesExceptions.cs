using Gameboard.Api.Features.Games;

namespace Gameboard.Api.Features.ExternalGames;

public class GameModeIsntExternal : GameboardException
{
    public GameModeIsntExternal(string gameId, string mode) : base($"Can't boot external game with id '{gameId}' because its mode ('{mode}') isn't set to '{GameMode.External}'.") { }
}
