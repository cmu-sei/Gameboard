using System;
using System.Collections.Generic;
using System.Linq;

namespace Gameboard.Api.Features.Reports;

public static class ReportsIQueryableExtensions
{
    public static IOrderedEnumerable<TSort> Sort<TSort, TKey>(this IEnumerable<TSort> enumerable, Func<TSort, TKey> orderBy, SortDirection sortDirection = SortDirection.Asc)
    {
        if (sortDirection == SortDirection.Asc)
            return enumerable.OrderBy(orderBy);

        return enumerable.OrderByDescending(orderBy);
    }
}
