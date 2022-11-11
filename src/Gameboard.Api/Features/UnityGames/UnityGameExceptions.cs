using System;

namespace Gameboard.Api.Features.UnityGames;

public class PlayerWrongGameIDException : Exception { }
public class TeamHasNoPlayersException : Exception { }

internal class ChallengeResolutionFailure : GameboardException
{
    public ChallengeResolutionFailure(string teamId) : base($"Couldn't resolve a Unity challenge for team {teamId}") { }
}

internal class SemaphoreLockFailure : GameboardException
{
    public SemaphoreLockFailure(Exception ex) : base($"An operation inside a semaphore lock failed.", ex) { }
}

internal class SpecNotFound : GameboardException
{
    public SpecNotFound(string gameId) : base($"Couldn't resolve a challenge spec for gameId {gameId}.") { }
}