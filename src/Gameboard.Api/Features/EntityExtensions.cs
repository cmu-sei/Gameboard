using System.Linq;

namespace Gameboard.Api.Features.Player;

internal static class PlayerExtensions
{
    public static IQueryable<Gameboard.Api.Data.Player> WhereIsScoringPlayer(this IQueryable<Gameboard.Api.Data.Player> query)
        => query.Where(p => p.Score > 0);
}
