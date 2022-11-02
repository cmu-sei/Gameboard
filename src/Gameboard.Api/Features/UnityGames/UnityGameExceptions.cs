using System;

namespace Gameboard.Api.Features.UnityGames;

public class PlayerWrongGameIDException : Exception { }
public class TeamHasNoPlayersException : Exception { }

internal class SemaphoreLockFailure : GameboardException
{
    public SemaphoreLockFailure(Exception ex) : base($"An operation inside a semaphore lock failed.", ex) { }
}

internal class SpecNotFound : GameboardException
{
    public SpecNotFound(string gameId) : base($"Couldn't resolve a challenge spec for gameId {gameId}.") { }
}