using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Teams;

internal class CantExtendUnstartedSession : GameboardValidationException
{
    internal CantExtendUnstartedSession(string teamId)
        : base($"Can't extend session for team {teamId}: Their session hasn't started.") { }
}

internal class CantJoinTeamBecausePlayerCount : GameboardValidationException
{
    public CantJoinTeamBecausePlayerCount(string gameId, int playersToJoin, int teamSizeCurrent, int teamSizeMin, int teamSizeMax)
        : base($"Can't add {playersToJoin} player(s) to the team. This team has {teamSizeCurrent} player(s) (min team size is {teamSizeMin}, max team size is {teamSizeMax}).") { }
}

internal class CaptainResolutionFailure : GameboardException
{
    internal CaptainResolutionFailure(string teamId, string message = null)
        : base($"Couldn't resolve a team captain for team {teamId}. {(message.IsEmpty() ? "" : message)}") { }
}

public class InvalidTeamSize : GameboardValidationException
{
    public InvalidTeamSize()
        : base("Invalid team size") { }

    public InvalidTeamSize(string teamId, int size, int min, int max)
        : base($"Team {teamId} has an invalid size (min: {min}, max {max}, current: {size})") { }
}

internal class NotOnSameTeam : GameboardException
{
    internal NotOnSameTeam(string firstPlayerId, string firstPlayerTeamId, string secondPlayerId, string secondPlayerTeamId, string whyItMatters) : base($"""
        Players {firstPlayerId} (team {firstPlayerTeamId}) and {secondPlayerId} (team {secondPlayerTeamId} aren't on the same team . {whyItMatters}
    """) { }
}

internal class PlayersAreFromMultipleTeams : GameboardValidationException
{
    internal PlayersAreFromMultipleTeams(IEnumerable<string> teamIds, string message = null) : base($"""{(string.IsNullOrWhiteSpace(message) ? "" : "{message}.")} The players evaluated are from zero or multiple teams ({string.Join(",", teamIds)})""") { }
}

internal class PlayersAreInMultipleGames : GameboardValidationException
{
    internal PlayersAreInMultipleGames(IEnumerable<string> gameIds) : base($"""The players have multiple dinstinct gameIds: ({string.Join(",", gameIds)})""") { }
}

internal class PlayerIsntOnTeam : GameboardValidationException
{
    internal PlayerIsntOnTeam(string playerId, string teamId, string playerTeamId, string message = null)
        : base($"""{(message.NotEmpty() ? $"{message}: " : string.Empty)}Player {playerId} isn't on team {teamId} (they're on team {playerTeamId}).""") { }
}

internal class TeamHasNoPlayersException : GameboardValidationException
{
    public TeamHasNoPlayersException(string teamId) : base($"Team {teamId} has no players.") { }
}

internal class UserIsntOnTeam : GameboardValidationException
{
    internal UserIsntOnTeam(string userId, string teamId, string message = null)
        : base($"""{(message.NotEmpty() ? $"{message}: " : string.Empty)}User {userId} isn't on team {teamId}.""") { }
}
