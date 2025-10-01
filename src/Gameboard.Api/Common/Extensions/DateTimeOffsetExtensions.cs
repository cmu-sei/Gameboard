// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Common;

public static class DateTimeOffsetExtensions
{
    public static bool IsNotEmpty(this DateTimeOffset ts)
    {
        return ts.Year > AppConstants.NULL_DATE.Year;
    }

    public static bool IsEmpty(this DateTimeOffset ts)
    {
        return ts.Year == AppConstants.NULL_DATE.Year;
    }

    public static bool IsNotEmpty(this DateTimeOffset? ts)
        => ts.HasValue && IsNotEmpty(ts.Value);

    public static bool IsEmpty(this DateTimeOffset? ts)
        => !ts.HasValue || IsEmpty(ts.Value);

    public static long? ToEpochMs(this DateTimeOffset ts)
        => ToEpochMs(new DateTimeOffset?(ts));

    public static long? ToEpochMs(this DateTimeOffset? ts)
        => ts.HasValue && ts.Value > DateTimeOffset.MinValue ? ts.Value.ToUnixTimeMilliseconds() : null;

    // public static bool WhereIsNotEmpty<TEntity>(this IQueryable<TEntity> query, Expression<Func<TEntity, DateTimeOffset>> propertyExpression)
    // {
    //     var entityPropertyParameter = Expression.Parameter(typeof(TEntity));

    //     var minDate = DateTimeOffset.MinValue;
    //     var minDateExp = Expression.Constant(minDate, typeof(DateTimeOffset));
    //     var dateTimeOffsetParameter = Expression.Parameter(typeof(TEntity));

    //     var callResult = Expression.Call(Expression.Equal(minDateExp, dateTimeOffsetParameter));
    //     var expression = Expression.Lambda
    //     (
    //         Expression.Equal(minDateExp, dateTimeOffsetParameter),
    //         false,
    //         dateTimeOffsetParameter
    //     );

    //     return query.Where()
    //     return query.Where(expression);

    //     // var expression = Expression<Func<TEntity, DateTimeOffset>>
    //     // return query.Where(Expression.Lambda<Func<TEntity, DateTimeOffset>>)
    // }
}
