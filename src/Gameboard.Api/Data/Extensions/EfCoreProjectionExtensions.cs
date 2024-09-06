using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Common;

public static class EfCoreProjectionExtensions
{
    public static async Task<IDictionary<TKey, IEnumerable<TValue>>> ToLookupAsync<TKey, TValue>(this IQueryable<TValue> query, Func<TValue, TKey> keyProperty, CancellationToken cancellationToken)
    {
        var result = await query.ToArrayAsync(cancellationToken);

        return result.ToDictionary(keyProperty, kv => kv.ToEnumerable());

    }
}
