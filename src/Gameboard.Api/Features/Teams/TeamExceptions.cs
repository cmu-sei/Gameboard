using System.Collections.Generic;
using Gameboard.Api.Common;

namespace Gameboard.Api.Features.Teams;

internal class CaptainResolutionFailure : GameboardException
{
    internal CaptainResolutionFailure(string teamId, string message = null)
        : base($"Couldn't resolve a team captain for team {teamId}. {(message.IsEmpty() ? "" : message)}") { }
}

internal class PlayersAreFromMultipleTeams : GameboardException
{
    internal PlayersAreFromMultipleTeams(IEnumerable<string> teamIds, string message = null) : base($"""${(string.IsNullOrWhiteSpace(message) ? "" : $"{message}.")} The players evaluated are from multiple teams ({string.Join(",", teamIds)})""") { }
}
