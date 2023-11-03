using System;
using System.Linq;
using System.Linq.Expressions;

namespace Gameboard.Api.Data;

public static class QueryExtensions
{
    /// <summary>
    /// Allows simplified evaluation of dates in the DB with value 01/01/0001.
    /// </summary>
    /// <typeparam name="T">The entity type upon which the query is based.</typeparam>
    /// <param name="query">An existing Linq query for entity type T.</param>
    /// <param name="dateExpression">An expression which resolves to a date property on T (e.g. e => e.StartDate).</param>
    /// <returns>A query with an appended `.Where` call that eliminates entities of type T with 01/01/0001 in the date field specified by `dateExpression`.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static IQueryable<T> WhereDateIsNotEmpty<T>(this IQueryable<T> query, Expression<Func<T, DateTimeOffset>> dateExpression) where T : class
        => WhereDate<T>(query, dateExpression, false);

    /// <summary>
    /// Allows simplified evaluation of dates in the DB with value 01/01/0001.
    /// </summary>
    /// <typeparam name="T">The entity type upon which the query is based.</typeparam>
    /// <param name="query">An existing Linq query for entity type T.</param>
    /// <param name="dateExpression">An expression which resolves to a date property on T (e.g. e => e.StartDate).</param>
    /// <returns>A query with an appended `.Where` call that eliminates entities of type T with 01/01/0001 in the date field specified by `dateExpression`.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static IQueryable<T> WhereDateIsEmpty<T>(this IQueryable<T> query, Expression<Func<T, DateTimeOffset>> dateExpression) where T : class
        => WhereDate<T>(query, dateExpression, true);

    private static IQueryable<T> WhereDate<T>(IQueryable<T> query, Expression<Func<T, DateTimeOffset>> dateExpression, bool isEmpty) where T : class
    {
        if (dateExpression == null)
            throw new ArgumentNullException(nameof(dateExpression));
        if (dateExpression.Body is not MemberExpression)
            throw new ArgumentException($"Can't use this extension with a {nameof(dateExpression)} argument which has a {nameof(dateExpression.Body)} property of a type not equal to {nameof(MemberExpression)}.");

        var entityParameter = Expression.Parameter(typeof(T));
        var accessMemberOnEntity = Expression.MakeMemberAccess(entityParameter, (dateExpression.Body as MemberExpression).Member);
        var finalExpression = Expression.Lambda<Func<T, bool>>
        (
            (
                isEmpty ?
                    Expression.Equal(accessMemberOnEntity, Expression.Constant(DateTimeOffset.MinValue)) :
                    Expression.NotEqual(accessMemberOnEntity, Expression.Constant(DateTimeOffset.MinValue))
            ), entityParameter
        );

        return query.Where(finalExpression);
    }

    public static IQueryable<Data.Challenge> WhereIsFullySolved(this IQueryable<Data.Challenge> query)
        => query.Where(c => c.Score >= c.Points);

    public static IQueryable<Player> WhereIsScoringPlayer(this IQueryable<Data.Player> query)
        => query.Where(p => p.Score > 0);
}
