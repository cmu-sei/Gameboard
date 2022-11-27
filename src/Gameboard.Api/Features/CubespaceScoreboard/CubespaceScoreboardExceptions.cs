using System.Collections.Generic;

namespace Gameboard.Api.Features.CubespaceScoreboard;

internal class GameResolutionFailure : GameboardException
{
    public GameResolutionFailure(IEnumerable<string> gameIds) : base($"Couldn't resolve the Cubespace game for the scoreboard. Candidate gameIds are: {string.Join(" | ", gameIds)}.") { }
}