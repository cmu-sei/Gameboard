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

internal class CantResolveTeamFromCode : GameboardValidationException
{
    internal CantResolveTeamFromCode(string code, string[] teamIds)
        : base($"""Couldn't resolve a unique team from invitation code "{code}": {teamIds.Length} have this code ({string.Join(",", teamIds)}). """) { }
}

internal class CaptainResolutionFailure : GameboardException
{
    internal CaptainResolutionFailure(string message = null)
        : base($"Couldn't resolve a team captain for team - it has no players. {(message.IsEmpty() ? "" : message)}") { }
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
    internal PlayersAreFromMultipleTeams(IEnumerable<string> teamIds, string message = null) : base($"""{(string.IsNullOrWhiteSpace(message) ? "" : $"{message}. ")}The players evaluated are from zero or multiple teams ({string.Join(",", teamIds)})""") { }
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

internal class RequiresSameSponsor : GameboardValidationException
{
    internal RequiresSameSponsor(string gameId, string managerPlayerId, string managerSponsor, string playerId, string playerSponsor)
        : base($"Game {gameId} requires that all players have the same sponsor. The inviting player {managerPlayerId} has sponsor {managerSponsor}, while player {playerId} has sponsor {playerSponsor}.") { }
}

internal class TeamHasNoPlayersException : GameboardValidationException
{
    public TeamHasNoPlayersException(string teamId) : base($"Team {teamId} has no players.") { }
}

internal class TeamsAreFromMultipleGames : GameboardException
{
    public TeamsAreFromMultipleGames(IEnumerable<string> teamIds, IEnumerable<string> gameIds)
        : base($"TeamIds {string.Join(',', teamIds)} represent players from multiple games {string.Join(',', gameIds)}.") { }
}

internal class TeamIsFull : GameboardValidationException
{
    internal TeamIsFull(string invitingPlayerId, int teamSize, int maxTeamSize)
        : base($"Inviting player {invitingPlayerId} has {teamSize} players on their team, and the max team size for this game is {maxTeamSize}.") { }
}

internal class UserIsntOnTeam : GameboardValidationException
{
    internal UserIsntOnTeam(string userId, string teamId, string message = null)
        : base($"""{(message.NotEmpty() ? $"{message}: " : string.Empty)}User {userId} isn't on team {teamId}.""") { }
}
