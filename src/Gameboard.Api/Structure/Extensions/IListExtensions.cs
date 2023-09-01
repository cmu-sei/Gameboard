using System;
using System.Collections.Generic;

namespace Gameboard.Api;

public static class IListExtensions
{
    public static IList<TItem> AddIf<TItem>(this IList<TItem> list, TItem item, Func<TItem, bool> condition) where TItem : class
    {
        if (condition(item))
            list.Add(item);

        return list;
    }

    public static IList<TItem> AddIfNotNull<TItem>(this IList<TItem> list, TItem item) where TItem : class
        => AddIf(list, item, item => item != default(TItem));
}
