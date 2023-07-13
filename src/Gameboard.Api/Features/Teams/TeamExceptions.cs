using System.Collections.Generic;
using Gameboard.Api.Common;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Teams;

internal class CaptainResolutionFailure : GameboardException
{
    internal CaptainResolutionFailure(string teamId, string message = null)
        : base($"Couldn't resolve a team captain for team {teamId}. {(message.IsEmpty() ? "" : message)}") { }
}

internal class NotOnSameTeam : GameboardException
{
    internal NotOnSameTeam(string firstPlayerId, string firstPlayerTeamId, string secondPlayerId, string secondPlayerTeamId, string whyItMatters) : base($"""
        Players {firstPlayerId} (team {firstPlayerTeamId}) and {secondPlayerId} (team {secondPlayerTeamId} aren't on the same team . {whyItMatters}
    """) { }
}

internal class PlayersAreFromMultipleTeams : GameboardException
{
    internal PlayersAreFromMultipleTeams(IEnumerable<string> teamIds, string message = null) : base($"""${(string.IsNullOrWhiteSpace(message) ? "" : $"{message}.")} The players evaluated are from multiple teams ({string.Join(",", teamIds)})""") { }
}

internal class PlayerIsntOnTeam : GameboardValidationException
{
    internal PlayerIsntOnTeam(string playerId, string teamId, string playerTeamId, string message = null)
        : base($"""{(message.NotEmpty() ? $"{message}: " : string.Empty)}Player {playerId} isn't on team {teamId} (they're on team {playerTeamId}).""") { }
}

internal class TeamHasNoPlayersException : GameboardException
{
    public TeamHasNoPlayersException(string teamId) : base($"Team {teamId} has no players.") { }
}

internal class UserIsntOnTeam : GameboardException
{
    internal UserIsntOnTeam(string userId, string teamId, string message = null)
        : base($"""{(message.NotEmpty() ? $"{message}: " : string.Empty)}User {userId} isn't on team {teamId}.""") { }
}
