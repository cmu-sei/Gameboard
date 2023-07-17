using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Gameboard.Api.Common;

namespace Gameboard.Api.Features.Reports;

public class PracticeModeReportGrouping
{
    public static readonly string Challenge = "challenge";
    public static readonly string Player = "player";
    public static readonly string PlayerPerformance = "player-performance";
}

[JsonDerivedType(typeof(PracticeModeByChallengeReportRecord), typeDiscriminator: "byChallenge")]
[JsonDerivedType(typeof(PracticeModeByUserReportRecord), typeDiscriminator: "byUser")]
[JsonDerivedType(typeof(PracticeModeReportByPlayerPerformanceRecord), typeDiscriminator: "byPlayerPerformance")]
public interface IPracticeModeReportRecord { }

public sealed class PracticeModeReportParameters
{
    public DateTimeOffset? AttemptDateStart { get; set; }
    public DateTimeOffset? AttemptDateEnd { get; set; }
    public ReportDateRange AttemptDateRange { get; set; }
    public IEnumerable<string> GameIds { get; set; }
    public IEnumerable<string> SponsorIds { get; set; }
    public string Grouping { get; set; }
}

public sealed class PracticeModeByUserReportRecord : IPracticeModeReportRecord
{
    public required PracticeModeReportUser User { get; set; }
    public required PracticeModeByUserReportChallenge Challenge { get; set; }
    public required IEnumerable<PracticeModeReportAttempt> Attempts { get; set; }
}

public sealed class PracticeModeReportUser
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required ReportSponsorViewModel Sponsor { get; set; }
    public required bool HasScoringAttempt { get; set; }
}

public sealed class PracticeModeByUserReportChallenge
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required ReportGameViewModel Game { get; set; }
    public required double MaxPossibleScore { get; set; }
}

public sealed class PracticeModeReportAttempt
{
    public required SimpleEntity Player { get; set; }
    public ReportTeamViewModel Team { get; set; }
    public required ReportSponsorViewModel Sponsor { get; set; }
    public required DateTimeOffset Start { get; set; }
    public required DateTimeOffset End { get; set; }
    public required double DurationMs { get; set; }
    public required double Score { get; set; }
    public required int PartiallyCorrectCount { get; set; }
    public required int FullyCorrectCount { get; set; }
}

public sealed class PracticeModeUserCompetitiveSummary
{
    public required double AvgCompetitivePointsPct { get; set; }
    public required int CompetitiveChallengesPlayed { get; set; }
    public required int CompetitiveGamesPlayed { get; set; }
    public required DateTimeOffset LastCompetitiveChallengeDate { get; set; }
}

public sealed class PracticeModeByChallengeReportRecord : IPracticeModeReportRecord
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required ReportGameViewModel Game { get; set; }
    public required double MaxPossibleScore { get; set; }
    public required double AvgScore { get; set; }
    public required string Description { get; set; }
    public required string Text { get; set; }
    public required IEnumerable<ReportSponsorViewModel> SponsorsPlayed { get; set; }
    public required PracticeModeReportByChallengePerformance OverallPerformance { get; set; }
    public required IEnumerable<PracticeModeReportByChallengePerformanceBySponsor> PerformanceBySponsor { get; set; }
}

public sealed class PracticeModeReportByChallengePerformance
{
    public required IEnumerable<string> Players { get; set; }
    public required int TotalAttempts { get; set; }
    public required decimal? ScoreHigh { get; set; }
    public required decimal? ScoreAvg { get; set; }
    public required int CompleteSolves { get; set; }
    public required decimal? PercentageCompleteSolved { get; set; }
    public required int PartialSolves { get; set; }
    public required decimal? PercentagePartiallySolved { get; set; }
    public required int ZeroScoreSolves { get; set; }
    public required decimal? PercentageZeroScoreSolved { get; set; }
}

public sealed class PracticeModeReportByChallengePerformanceBySponsor
{
    public required ReportSponsorViewModel Sponsor { get; set; }
    public required PracticeModeReportByChallengePerformance Performance { get; set; }
}

public sealed class PracticeModeReportByPlayerPerformanceRecord : IPracticeModeReportRecord
{
    public required SimpleEntity Player { get; set; }
    public required ReportSponsorViewModel Sponsor { get; set; }
    public required PracticeModeReportByPlayerPerformanceModeSummary PracticeStats { get; set; }
    public required PracticeModeReportByPlayerPerformanceModeSummary CompetitiveStats { get; set; }
}

public sealed class PracticeModeReportByPlayerPerformanceModeSummary
{
    public required DateTimeOffset? LastAttemptDate { get; set; }
    public required int TotalChallengesPlayed { get; set; }
    public required decimal ZeroScoreSolves { get; set; }
    public required decimal PartialSolves { get; set; }
    public required decimal CompleteSolves { get; set; }
    public required decimal AvgPctAvailablePointsScored { get; set; }
    public required decimal AvgScorePercentile { get; set; }
}

internal sealed class PracticeModeReportByPlayerPerformanceChallengeScore
{
    public required string ChallengeSpecId { get; set; }
    public required string UserId { get; set; }
    public required bool IsPractice { get; set; }
    public required decimal Score { get; set; }
}
