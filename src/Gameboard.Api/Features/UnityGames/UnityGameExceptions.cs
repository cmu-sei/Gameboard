using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Gameboard.Api.Features.UnityGames;

public class PlayerWrongGameIDException : Exception { }
public class TeamHasNoPlayersException : Exception { }

internal class ChallengeResolutionFailure : GameboardException
{
    public ChallengeResolutionFailure(string teamId, IEnumerable<string> challengeIds) : base($"Couldn't resolve a Unity challenge for team {teamId}. They have {challengeIds.Count()} challenges ({String.Join(" | ", challengeIds)})") { }
}

internal class GamebrainException : GameboardException
{
    public GamebrainException(HttpMethod method, string endpoint, HttpStatusCode statusCode, string error) : base($"Gamebrain threw a {statusCode} in response to a {method} request to {endpoint}. Error detail: '{error}'") { }
}

internal class GamebrainEmptyResponseException : GameboardException
{
    public GamebrainEmptyResponseException(HttpMethod method, string url) : base($"Gamebrain didn't respond to a {method} request to {url}.") { }
}

internal class SemaphoreLockFailure : GameboardException
{
    public SemaphoreLockFailure(Exception ex) : base($"An operation inside a semaphore lock failed.", ex) { }
}

internal class SpecNotFound : GameboardException
{
    public SpecNotFound(string gameId) : base($"Couldn't resolve a challenge spec for gameId {gameId}.") { }
}
