using System.Linq;

namespace Gameboard.Api.Data;

public static class QueryHelperExtensions
{
    public static IQueryable<Data.Challenge> WhereIsFullySolved(this IQueryable<Data.Challenge> query)
    {
        return query
            .Where(c => c.Score >= c.Points);
    }
}
