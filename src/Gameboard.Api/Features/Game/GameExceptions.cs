using System;
using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Games;

internal class CantDeleteGameWithPlayers : GameboardValidationException
{
    public CantDeleteGameWithPlayers(string gameId, int playerCount)
        : base($"Game {gameId} can't be deleted because it has {playerCount} players.") { }
}

internal class CantStartGameWithNoPlayers : GameboardException
{
    public CantStartGameWithNoPlayers(string gameId) : base($"Can't start game {gameId} - no players are registered.") { }
}

internal class CantSynchronizeNonSynchronizedGame : GameboardValidationException
{
    public CantSynchronizeNonSynchronizedGame(string gameId) : base($"Can't enforce synchronized start on non-synchronized game \"{gameId}\".") { }
}

internal class CantStartNonReadySynchronizedGame : GameboardValidationException
{
    public CantStartNonReadySynchronizedGame(SyncStartState state) : base($"Can't start synchronized game \"{state.Game.Id}\" - {GetNonReadyPlayersFromState(state).Count()} players aren't ready (\"{string.Join(",", GetNonReadyPlayersFromState(state).Select(p => p.Id))}\").") { }

    private static IEnumerable<SyncStartPlayer> GetNonReadyPlayersFromState(SyncStartState state)
        => state.Teams.SelectMany(t => t.Players).Where(p => !p.IsReady);
}

internal class CantStartStandardGameWithoutActingUserParameter : GameboardValidationException
{
    public CantStartStandardGameWithoutActingUserParameter(string gameId) : base($"""Game start failure (gameId "{gameId}"): Game is a standard game, so the `actingUser` parameter is required.""") { }
}

internal class ChallengeResolutionFailure : GameboardException
{
    public ChallengeResolutionFailure(string teamId, IEnumerable<string> challengeIds) : base($"Couldn't resolve a Unity challenge for team {teamId}. They have {challengeIds.Count()} challenges ({String.Join(" | ", challengeIds)})") { }
}

internal class EmptyExternalStartupUrl : GameboardException
{
    public EmptyExternalStartupUrl(string gameId, string startupUrl) : base($"""Game ${gameId} doesn't have a configured {nameof(Game.ExternalGameStartupUrl)} configured (current value: "{startupUrl}")""") { }
}

internal class GameIsNotSyncStart : GameboardValidationException
{
    public GameIsNotSyncStart(string gameId, string whyItMatters) : base($"""Game "{gameId}" is not a sync-start game. {whyItMatters}""") { }
}

public class GameModeIsntExternal : GameboardValidationException
{
    public GameModeIsntExternal(string gameId, string mode) : base($"Can't boot external game with id '{gameId}' because its mode ('{mode}') isn't set to '{GameEngineMode.External}'.") { }
}

public class GameDoesntAllowReset : GameboardValidationException
{
    public GameDoesntAllowReset(string gameId) : base($"""Game {gameId} has "Allow Reset" set to disabled.""") { }
}

internal class UserIsntPlayingGame : GameboardValidationException
{
    public UserIsntPlayingGame(string userId, string gameId, string whyItMatters = null) : base($"""User {userId} isn't playing game {gameId}.{(string.IsNullOrWhiteSpace(whyItMatters) ? string.Empty : ". " + whyItMatters)} """) { }
}

public class PlayerWrongGameIDException : Exception { }

internal class PracticeSessionLimitReached : GameboardValidationException
{
    public PracticeSessionLimitReached(string userId, int userSessionCount, int practiceSessionLimit) : base($"Can't start a new practice session. User \"{userId}\" has \"{userSessionCount}\" practice sessions, and the limit is \"{practiceSessionLimit}\".") { }
}

internal class SessionLimitReached : GameboardValidationException
{
    public SessionLimitReached(string teamId, string gameId, int sessions, int sessionLimit) : base($"Can't start a new game ({gameId}) for team \"{teamId}\". The session limit is {sessionLimit}, and the team has {sessions} sessions.") { }
}

internal class SpecNotFound : GameboardException
{
    public SpecNotFound(string gameId) : base($"Couldn't resolve a challenge spec for gameId {gameId}.") { }
}

internal class SynchronizedGameHasPlayersWithChallengesBeforeStart : GameboardValidationException
{
    public SynchronizedGameHasPlayersWithChallengesBeforeStart(string gameId, IEnumerable<string> playerIdsWithChallenges)
        : base($"Can't launch synchronized game {gameId}. {playerIdsWithChallenges} players already have a game session ({string.Join(", ", playerIdsWithChallenges)})") { }
}

internal class SynchronizedGameHasPlayersWithSessionsBeforeStart : GameboardValidationException
{
    public SynchronizedGameHasPlayersWithSessionsBeforeStart(string gameId, IEnumerable<string> playerIdsWithSessions)
        : base($"""Can't launch synchronized game "{gameId}". {playerIdsWithSessions.Count()} players already have a game session: ("{string.Join(",", playerIdsWithSessions)}") """) { }
}
