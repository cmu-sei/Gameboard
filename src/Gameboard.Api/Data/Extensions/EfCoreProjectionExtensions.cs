using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Common;

public static class EfCoreProjectionExtensions
{
    public static async Task<IDictionary<TKey, List<TValue>>> ToLookupAsync<TKey, TValue>(this IQueryable<TValue> query, Func<TValue, TKey> keyProperty, CancellationToken cancellationToken)
    {
        var resultList = await query.ToArrayAsync(cancellationToken);
        var retVal = new Dictionary<TKey, List<TValue>>();

        foreach (var result in resultList)
        {
            var keyValue = keyProperty(result);
            if (retVal.TryGetValue(keyValue, out var existingList))
            {
                existingList.Add(result);
            }
            else
            {
                retVal.Add(keyValue, new List<TValue>([result]));
            }
        }

        return retVal;
    }
}
