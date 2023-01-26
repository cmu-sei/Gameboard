using System;

namespace Gameboard.Api.Features.Player;

internal class InvalidExtendSessionRequest : GameboardException
{
    internal InvalidExtendSessionRequest(DateTimeOffset currentSessionEnd, DateTimeOffset requestedSessionEnd)
        : base($"Can't extend the session: The current session ends at {currentSessionEnd.ToString("u")}, and the request would extend it to {requestedSessionEnd.ToString("u")} (before the current session is set to end).") { }
}

internal class SessionNotActive : GameboardException
{
    internal SessionNotActive(string playerId) : base($"Player {playerId} has an inactive session.") { }
}

internal class TeamIsFull : GameboardException
{
    internal TeamIsFull(string invitingPlayerId, int teamSize, int maxTeamSize)
        : base($"Inviting player {invitingPlayerId} has {teamSize} players on their team, and the max team size for this game is {maxTeamSize}.") { }
}