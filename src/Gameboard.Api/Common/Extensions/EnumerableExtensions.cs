// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Gameboard.Api.Common;

public static class EnumerableExtensions
{
    public static bool IsNotEmpty<T>(this IEnumerable<T> enumerable)
        => enumerable is not null && enumerable.Any();

    public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
        => !IsNotEmpty(enumerable);

    public static IOrderedEnumerable<TSort> Sort<TSort, TKey>(this IEnumerable<TSort> enumerable, Func<TSort, TKey> orderBy, SortDirection? sortDirection = null)
    {
        if (sortDirection == SortDirection.Desc)
            return enumerable.OrderByDescending(orderBy);

        return enumerable.OrderBy(orderBy);
    }

    public static IOrderedQueryable<TSort> Sort<TSort, TKey>(this IQueryable<TSort> query, Expression<Func<TSort, TKey>> orderBy, SortDirection? sortDirection = SortDirection.Asc)
    {
        if (sortDirection == SortDirection.Desc)
            return query.OrderByDescending(orderBy);

        return query.OrderBy(orderBy);
    }

    public static IEnumerable<T> ToEnumerable<T>(this T thing)
        => [thing];

    // for logging
    public static string ToDelimited(this IEnumerable<string> enumerable, string delimiter = ", ")
        => string.Join(delimiter, enumerable);
}
