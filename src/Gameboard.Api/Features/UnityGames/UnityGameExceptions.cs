using System;

namespace Gameboard.Api.Features.UnityGames;

public class PlayerWrongGameIDException : Exception { }
public class TeamHasNoPlayersException : Exception { }

internal class SpecNotFound : GameboardException
{
    public SpecNotFound(string gameId) : base($"Couldn't resolve a challenge spec for gameId {gameId}.") { }
}