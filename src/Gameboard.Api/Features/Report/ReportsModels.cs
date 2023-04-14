using System;
using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Reports;

public interface IReportResult<T>
{
    public ReportMetaData MetaData { get; set; }
    public IEnumerable<T> Records { get; set; }
}

public sealed class ReportMetaData
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required DateTimeOffset RunAt { get; set; }
}

public sealed class ReportDateRange
{
    public DateTimeOffset DateStart { get; set; }
    public DateTimeOffset DateEnd { get; set; }
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
