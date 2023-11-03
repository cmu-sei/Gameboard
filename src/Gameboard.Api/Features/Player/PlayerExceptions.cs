using System;
using System.Collections.Generic;
using System.Linq;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Player;

internal class CantResolveTeamFromCode : GameboardException
{
    internal CantResolveTeamFromCode(string code, string[] teamIds) : base($"""Couldn't resolve a unique team from invitation code "{code}": {teamIds.Count()} have this code ({string.Join(",", teamIds)}). """) { }
}

internal class InvalidExtendSessionRequest : GameboardException
{
    internal InvalidExtendSessionRequest(DateTimeOffset currentSessionEnd, DateTimeOffset requestedSessionEnd)
        : base($"Can't extend the session: The current session ends at {currentSessionEnd:u}, and the request would extend it to {requestedSessionEnd:u} (before the current session is set to end).") { }
}

internal class ManagerCantUnenrollWhileTeammatesRemain : GameboardValidationException
{
    internal ManagerCantUnenrollWhileTeammatesRemain(string playerId, string teamId, IEnumerable<string> teammatePlayerIds) : base($"""
        You're currently the manager of this team. There are currently {teammatePlayerIds.Count()} other player(s) remaining on the team ({string.Join(" | ", teammatePlayerIds)}).
        In order to unenroll, you'll need to designate a teammate as the replacement manager (or wait until all other team members have unenrolled).
    """) { }
}

internal class CantEnrollWithDefaultSponsor : GameboardValidationException
{
    internal CantEnrollWithDefaultSponsor(string userId, string gameId) : base($"""User "{userId}" can't enroll in game "{gameId}": User still has the default sponsor. """) { }
}

internal class NoPlayerSponsorForGame : GameboardValidationException
{
    internal NoPlayerSponsorForGame(string userId, string gameId) : base($"""User "{userId}" hasn't selected a sponsor, so they can't register for game "{gameId}".""") { }
}

internal class PlayerIsntManager : GameboardException
{
    internal PlayerIsntManager(string playerId, string addlMessage) : base($"Player {playerId} isn't the team manager. {addlMessage}") { }
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
