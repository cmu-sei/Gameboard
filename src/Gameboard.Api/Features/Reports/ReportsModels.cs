using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Reports;

public static class ReportKey
{
    public static string Challenges { get; } = "challenges";
    public static string Enrollment { get; } = "enrollment";
    public static string Players { get; } = "players";
    public static string PracticeArea { get; } = "practice-area";
    public static string Support { get; } = "support";
}

public interface IReportQuery
{
    public User ActingUser { get; }
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

public sealed class ReportRawResults<TRecord>
{
    public required PagingArgs PagingArgs { get; set; }
    public required string ParameterSummary { get; set; }
    public required IEnumerable<TRecord> Records { get; set; }
    public required string ReportKey { get; set; }
    public required string Title { get; set; }
}

public sealed class ReportRawResults<TOverallStats, TRecord>
{
    public required TOverallStats OverallStats { get; set; }
    public required PagingArgs PagingArgs { get; set; }
    public required string ParameterSummary { get; set; }
    public required IEnumerable<TRecord> Records { get; set; }
    public required string ReportKey { get; set; }
    public required string Title { get; set; }
}

public sealed class ReportDateRange
{
    public DateTimeOffset? DateStart { get; set; } = null;
    public bool HasDateStartValue { get => DateStart != null && ((DateTimeOffset)DateStart).HasValue(); }

    public DateTimeOffset? DateEnd { get; set; } = null;
    public bool HasDateEndValue { get => DateEnd != null && ((DateTimeOffset)DateStart).HasValue(); }
}

public sealed class ReportResults<TRecord>
{
    public required ReportMetaData MetaData { get; set; }
    public required PagingResults Paging { get; set; }
    public required IEnumerable<TRecord> Records { get; set; }
}

public sealed class ReportResults<TOverallStats, TRecord>
{
    public required ReportMetaData MetaData { get; set; }
    public required PagingResults Paging { get; set; }
    public required IEnumerable<TRecord> Records { get; set; }
    public required TOverallStats OverallStats { get; set; }
}
