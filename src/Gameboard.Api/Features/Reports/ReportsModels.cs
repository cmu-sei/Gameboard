using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Gameboard.Api.Common;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Reports;

public static class ReportKey
{
    public static string Challenges { get; } = "challenges";
    public static string Enrollment { get; } = "enrollment";
    public static string Players { get; } = "players";
    public static string PracticeMode { get; } = "practice-mode";
    public static string Support { get; } = "support";
}

public sealed class ReportViewModel
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required IEnumerable<string> ExampleFields { get; set; }
    public required IEnumerable<string> ExampleParameters { get; set; }
}

public sealed class ReportMetaData
{
    public required string Key { get; set; }
    public required string Title { get; set; }
    public string ParametersSummary { get; set; }
    public required DateTimeOffset RunAt { get; set; }
}

public interface IReportPredicateProvider<TProperty>
{
    IEnumerable<Expression<Func<TProperty, bool>>> GetPredicates<TData>(TProperty entityExpression) where TData : IEntity;
}

public sealed class ReportDateRange // : IReportPredicateProvider<DateTimeOffset>
{
    public DateTimeOffset? DateStart { get; set; } = null;
    public bool HasDateStartValue { get => DateStart != null && ((DateTimeOffset)DateStart).HasValue(); }

    public DateTimeOffset? DateEnd { get; set; } = null;
    public bool HasDateEndValue { get => DateEnd != null && ((DateTimeOffset)DateStart).HasValue(); }

    // public IEnumerable<Expression<Func<DateTimeOffset, bool>>> GetPredicates<TData>(Func<TData, DateTimeOffset> value) where TData : IEntity
    // {
    //     if (DateStart != null)
    //         yield return d => new Expression<Func<TData, DateTimeOffset>>(d => value(d) >= DateStart.Value);

    //     if (DateEnd != null)
    //         yield return d => d <= DateEnd.Value;
    // }
}

public sealed class ReportScoreRange : IReportPredicateProvider<double>
{
    public double? Min { get; set; } = null;
    public double? Max { get; set; } = null;

    public IEnumerable<Expression<Func<double, bool>>> GetPredicates<TData>(double entityExpression) where TData : IEntity
    {
        if (Min != null)
            yield return v => v >= Min.Value;

        if (Max != null)
            yield return v => v <= Max.Value;
    }
}

public sealed class ReportResults<TRecord>
{
    public required ReportMetaData MetaData { get; set; }
    public required PagingResults Paging { get; set; }
    public required IEnumerable<TRecord> Records { get; set; }
}
