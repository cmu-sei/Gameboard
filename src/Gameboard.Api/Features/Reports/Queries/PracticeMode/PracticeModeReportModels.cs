using System;
using System.Collections.Generic;
using Gameboard.Api.Common;

namespace Gameboard.Api.Features.Reports;

public class PracticeModeReportGrouping
{
    public static readonly string Challenge = "challenge";
    public static readonly string Player = "player";
}

public sealed class PracticeModeReportParameters
{
    public ReportDateRange AttemptDateRange { get; set; }
    public IEnumerable<string> GameIds { get; set; }
    public IEnumerable<string> SponsorIds { get; set; }
}

public sealed class PracticeModeReportRecord
{
    public required SimpleEntity Game { get; set; }
    public required SimpleEntity Challenge { get; set; }
    public required PracticeModeReportPlayer Player { get; set; }
}

public sealed class PracticeModeReportPlayer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset? EnrollDate { get; set; }
    public required ReportSponsorViewModel Sponsor { get; set; }
    public required IEnumerable<PracticeModeReportPlayTime> Attempts { get; set; }
}

public sealed class PracticeModeReportPlayTime
{
    public required DateTimeOffset Start { get; set; }
    public required DateTimeOffset End { get; set; }
    public required double DurationMs { get; set; }
}
