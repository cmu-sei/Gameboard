using System;
using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Common;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Player;

internal class InvalidExtendSessionRequest : GameboardException
{
    internal InvalidExtendSessionRequest(DateTimeOffset currentSessionEnd, DateTimeOffset requestedSessionEnd)
        : base($"Can't extend the session: The current session ends at {currentSessionEnd.ToString("u")}, and the request would extend it to {requestedSessionEnd.ToString("u")} (before the current session is set to end).") { }
}

internal class GameDoesntAllowSessionReset : GameboardException
{
    internal GameDoesntAllowSessionReset(string playerId, string gameId, DateTimeOffset sessionStartedOn) : base($"Player {playerId} is playing Game {gameId}. This game doesn't allow non-administrative resets after a session has begun, and their session began at {sessionStartedOn}.") { }
}

internal class ManagerCantUnenrollWhileTeammatesRemain : GameboardException
{
    internal ManagerCantUnenrollWhileTeammatesRemain(string playerId, string teamId, IEnumerable<string> teammatePlayerIds) : base($"""
        Player {playerId} is the manager of team {teamId}. There are currently {teammatePlayerIds.Count()} players remaining on the team ({string.Join(" | ", teammatePlayerIds)}).
        In order to unenroll, this player must designate a teammate as the replacement manager (or wait until all other team memberes have unenrolled).
    """) { }
}

internal class NotOnSameTeam : GameboardException
{
    internal NotOnSameTeam(string firstPlayerId, string firstPlayerTeamId, string secondPlayerId, string secondPlayerTeamId, string whyItMatters) : base($"""
        Players {firstPlayerId} (team {firstPlayerTeamId}) and {secondPlayerId} (team {secondPlayerTeamId} aren't on the same team . {whyItMatters}
    """) { }
}

internal class NotManager : GameboardException
{
    internal NotManager(string playerId, string addlMessage) : base($"Player {playerId} isn't the team manager. {addlMessage}") { }
}

internal class PromotionFailed : GameboardException
{
    internal PromotionFailed(string teamId, string playerId, int recordsAffected) : base($"Failed to promote player {playerId} to manager of team {teamId}: Incorrect number of records affected ({recordsAffected}).") { }
}

internal class SessionAlreadyStarted : GameboardValidationException
{
    internal SessionAlreadyStarted(string playerId, string why) : base($"Player {playerId}'s session was started. {why}.") { }
}

internal class SessionNotActive : GameboardException
{
    internal SessionNotActive(string playerId) : base($"Player {playerId} has an inactive session.") { }
}

internal class SyncStartNotReady : GameboardValidationException
{
    public SyncStartNotReady(string playerId, SyncStartState state) : base($"Can't create a challenge for playerId '{playerId}'. The game requires synchronized start, and not all registered players are ready. Non-ready players: " + BuildPlayerSummary(state)) { }

    private static string BuildPlayerSummary(SyncStartState state)
    {
        var nonReadyPlayers = state
            .Teams
            .SelectMany(t => t.Players).Where(p => !p.IsReady);

        return string
            .Join("\n- ", nonReadyPlayers.Select(p => $"{p.Name} (id: {p.Id})").ToArray());
    }
}

internal class TeamIsFull : GameboardException
{
    internal TeamIsFull(string invitingPlayerId, int teamSize, int maxTeamSize)
        : base($"Inviting player {invitingPlayerId} has {teamSize} players on their team, and the max team size for this game is {maxTeamSize}.") { }
}
