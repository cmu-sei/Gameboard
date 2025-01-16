using System;
using System.Collections.Generic;
using System.Linq;

namespace Gameboard.Api.Features.Teams;

public static class TeamQueryExtensions
{
    // #just553things
    public static IQueryable<string> SelectedStartedTeamIds(this IQueryable<Data.Player> playerQuery)
    {
        return playerQuery
            .GroupBy(p => p.TeamId)
            .Where(gr => gr.Any(p => p.SessionBegin > DateTimeOffset.MinValue))
            .Select(gr => gr.Key);
    }

    public static IEnumerable<Data.Player> WhereTeamStarted(this IEnumerable<Data.Player> players)
    {
        return players
            .GroupBy(p => p.TeamId)
            .Where(gr => gr.Any(p => p.SessionBegin > DateTimeOffset.MinValue))
            .SelectMany(gr => gr);
    }
}
