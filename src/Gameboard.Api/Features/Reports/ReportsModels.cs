using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Reports;

public static class ReportKey
{
    public static string Challenges { get; } = "challenges";
    public static string Enrollment { get; } = "enrollment";
    public static string Feedback { get; } = "feedback";
    public static string Players { get; } = "players";
    public static string PracticeArea { get; } = "practice-area";
    public static string SiteUsage { get; } = "site-usage";
    public static string Support { get; } = "support";
}

public interface IReportQuery { }

public sealed class ReportViewModel
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsExportable { get; set; } = true;
    public required IEnumerable<string> ExampleFields { get; set; }
    public required IEnumerable<string> ExampleParameters { get; set; }
}

public sealed class ReportMetaData
{
    public required string Key { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public bool IsExportable { get; set; } = true;
    public string ParametersSummary { get; set; }
    public required DateTimeOffset RunAt { get; set; }
}

public sealed class ReportRawResults<TRecord>
{
    public required PagingArgs PagingArgs { get; set; }
    public required string Description { get; set; }
    public required string Key { get; set; }
    public required string ParameterSummary { get; set; }
    public required IEnumerable<TRecord> Records { get; set; }
    public required string Title { get; set; }
}

public sealed class ReportRawResults<TOverallStats, TRecord>
{
    public required TOverallStats OverallStats { get; set; }
    public required PagingArgs PagingArgs { get; set; }
    public required string Description { get; set; }
    public required string ParameterSummary { get; set; }
    public required string Key { get; set; }
    public required IEnumerable<TRecord> Records { get; set; }
    public required string Title { get; set; }
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
