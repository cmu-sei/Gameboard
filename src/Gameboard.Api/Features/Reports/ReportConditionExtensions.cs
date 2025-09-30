// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Gameboard.Api.Features.Reports;

internal static class ReportConditionExtensions
{
    public static IQueryable<TQuery> WhereMeetsCriteria<TQuery, TProperty>(this IQueryable<TQuery> queryable, Func<TQuery, TProperty> propertyValue, IEnumerable<Func<TProperty, bool>> criteria)
    {
        foreach (var criterion in criteria)
        {
            queryable = queryable.Where(q => criterion(propertyValue(q)));
        }

        return queryable;
    }

    public static IQueryable<T> WithConditions<T>(this IQueryable<T> queryable, params Expression<Func<T, bool>>[] conditions)
    {
        return queryable.WithConditions(conditions.AsEnumerable());
    }

    public static IQueryable<T> WithConditions<T>(this IQueryable<T> queryable, IEnumerable<Expression<Func<T, bool>>> conditions)
    {
        foreach (var condition in conditions)
            queryable = queryable.Where(condition).AsQueryable();

        return queryable;
    }
}
