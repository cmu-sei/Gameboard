using System.Linq;

internal static class PlayerExtensions
{
    public static IQueryable<Gameboard.Api.Data.Player> WhereIsScoringPlayer(this IQueryable<Gameboard.Api.Data.Player> query)
        => query.Where(p => p.Score > 0);
}
