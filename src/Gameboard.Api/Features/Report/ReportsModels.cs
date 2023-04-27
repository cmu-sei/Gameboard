using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Gameboard.Api.Data;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Reports;

public static class ReportKey
{
    public static string ChallengesReport { get; } = "challenges-report";
    public static string PlayersReport { get; } = "players-report";
}

public class ReportViewModel
{
    public required string Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required IEnumerable<string> ExampleFields { get; set; }
    public required IEnumerable<string> ExampleParameters { get; set; }
}

public interface IReportResult<T>
{
    public ReportMetaData MetaData { get; set; }
    public IEnumerable<T> Records { get; set; }
}

public sealed class ReportMetaData
{
    public required string Id { get; set; }
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
    public DateTimeOffset? DateEnd { get; set; } = null;

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

public class ParticipationReportArgs
{
    public string ChallengeId { get; set; }
    public string Competition { get; set; }
    public ReportDateRange DateRange { get; set; }
    public string GameId { get; set; }
    public string PlayerId { get; set; }
    public string SponsorId { get; set; }
    public string TeamId { get; set; }
    public string Track { get; set; }
}

public class ReportParameterOptions
{
    public required IEnumerable<SimpleEntity> Challenges { get; set; }
    public required IEnumerable<string> Competitions { get; set; }
    public required IEnumerable<SimpleEntity> Games { get; set; }
    public required IEnumerable<SimpleEntity> Players { get; set; }
    public required IEnumerable<SimpleEntity> Sponsors { get; set; }
    public required IEnumerable<SimpleEntity> Teams { get; set; }
    public required IEnumerable<string> Tracks { get; set; }
}

public class ReportParameters
{
    public string ChallengeId { get; set; }
    public string Competition { get; set; }
    public string GameId { get; set; }
    public string PlayerId { get; set; }
    public string SponsorId { get; set; }
    public string TeamId { get; set; }
    public string Track { get; set; }
}

public class ParticipationReportRecord
{
    public Sponsor Sponsor { get; set; }
    public SimpleEntity User { get; set; }
    public SimpleEntity Game { get; set; }
    public SimpleEntity Challenge { get; set; }
    public SimpleEntity Player { get; set; }
    public SimpleEntity Team { get; set; }
    public double CorrectCount { get; set; }
    public double PartialCount { get; set; }
}

public class ParticipationReport : IReportResult<ParticipationReportRecord>
{
    public required ReportMetaData MetaData { get; set; }
    public required IEnumerable<ParticipationReportRecord> Records { get; set; }
}
