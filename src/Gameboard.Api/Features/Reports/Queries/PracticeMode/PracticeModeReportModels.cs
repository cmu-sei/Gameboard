using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json.Serialization;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Reports;

public class PracticeModeReportGrouping
{
    public static readonly string Challenge = "challenge";
    public static readonly string Player = "player";
    public static readonly string PlayerModePerformance = "player-mode-performance";
}

public sealed class PracticeModeReportOverallStats
{
    public required int ChallengeCount { get; set; }
    public required int PlayerCount { get; set; }
    public required int SponsorCount { get; set; }
    public required int AttemptCount { get; set; }
    public required int CompletionCount { get; set; }
}

[JsonDerivedType(typeof(PracticeModeByChallengeReportRecord), typeDiscriminator: "byChallenge")]
[JsonDerivedType(typeof(PracticeModeByUserReportRecord), typeDiscriminator: "byUser")]
[JsonDerivedType(typeof(PracticeModeReportByPlayerModePerformanceRecord), typeDiscriminator: "byPlayerModePerformance")]
public interface IPracticeModeReportRecord { }

public sealed class PracticeModeReportResults
{
    public PracticeModeReportOverallStats OverallStats { get; set; }
    public IEnumerable<IPracticeModeReportRecord> Records { get; set; }
}

public sealed class PracticeModeReportParameters
{
    public DateTimeOffset? PracticeDateStart { get; set; }
    public DateTimeOffset? PracticeDateEnd { get; set; }
    public string Games { get; set; }
    public string Grouping { get; set; }
    public string Seasons { get; set; }
    public string Series { get; set; }
    public string Sponsors { get; set; }
    public string Tracks { get; set; }

    public string Sort { get; set; }
    public SortDirection SortDirection { get; set; }
}

public sealed class PracticeModeReportChallengeDetailParameters
{
    public ChallengeResult? PlayersWithSolveType { get; set; }
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
    public required IEnumerable<string> Tags { get; set; }
}

public sealed class PracticeModeReportAttempt
{
    public required SimpleEntity Player { get; set; }
    public ReportTeamViewModel Team { get; set; }
    public required ReportSponsorViewModel Sponsor { get; set; }
    public required DateTimeOffset Start { get; set; }
    public required DateTimeOffset End { get; set; }
    public required double DurationMs { get; set; }
    public required ChallengeResult Result { get; set; }
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
    public required IEnumerable<string> Tags { get; set; }
    public required string Text { get; set; }
    public required IEnumerable<ReportSponsorViewModel> SponsorsPlayed { get; set; }
    public required PracticeModeReportByChallengePerformance OverallPerformance { get; set; }
    public required IEnumerable<PracticeModeReportByChallengePerformanceBySponsor> PerformanceBySponsor { get; set; }
}

public sealed class PracticeModeReportByChallengePerformance
{
    public required IEnumerable<SimpleEntity> Players { get; set; }
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

public sealed class PracticeModeReportByPlayerModePerformanceRecord : IPracticeModeReportRecord
{
    public required SimpleEntity Player { get; set; }
    public required ReportSponsorViewModel Sponsor { get; set; }
    public required PracticeModeReportByPlayerModePerformanceRecordModeSummary PracticeStats { get; set; }
    public required PracticeModeReportByPlayerModePerformanceRecordModeSummary CompetitiveStats { get; set; }
}

public sealed class PracticeModeReportByPlayerModePerformanceRecordModeSummary
{
    public required DateTimeOffset? LastAttemptDate { get; set; }
    public required int TotalChallengesPlayed { get; set; }
    public required decimal ZeroScoreSolves { get; set; }
    public required decimal PartialSolves { get; set; }
    public required decimal CompleteSolves { get; set; }
    public required double AvgPctAvailablePointsScored { get; set; }
    public required decimal AvgScorePercentile { get; set; }
}

internal sealed class PracticeModeReportByPlayerModePerformanceChallengeScore
{
    public required string ChallengeId { get; set; }
    public required string ChallengeSpecId { get; set; }
    public required bool IsPractice { get; set; }
    public required double Score { get; set; }
}

public sealed class PracticeModeReportPlayerModeSummary
{
    public required PracticeModeReportUser Player { get; set; }
    public required IEnumerable<PracticeModeReportPlayerModeSummaryChallenge> Challenges { get; set; }
}

public sealed class PracticeModeReportPlayerModeSummaryChallenge
{
    public required SimpleEntity ChallengeSpec { get; set; }
    public required ReportGameViewModel Game { get; set; }
    public required decimal Score { get; set; }
    public required decimal MaxPossibleScore { get; set; }
    public required ChallengeResult Result { get; set; }
    public required double PctAvailablePointsScored { get; set; }
    public required decimal? ScorePercentile { get; set; }
}

public sealed class PracticeModeReportCsvRecord
{
    public required string UserId { get; set; }
    public required string UserName { get; set; }
    public required string ChallengeId { get; set; }
    public required string ChallengeName { get; set; }
    public required string ChallengeSpecId { get; set; }
    public required string GameId { get; set; }
    public required string GameName { get; set; }
    public required string PlayerId { get; set; }
    public required string PlayerName { get; set; }
    public required string SponsorId { get; set; }
    public required string SponsorName { get; set; }
    public required string TeamId { get; set; }
    public required string TeamName { get; set; }
    public required double Score { get; set; }
    public required double MaxPossibleScore { get; set; }
    public required double PctMaxPointsScored { get; set; }
    public required double? ScorePercentile { get; set; }
    public required DateTimeOffset? SessionStart { get; set; }
    public required DateTimeOffset? SessionEnd { get; set; }
    public required ChallengeResult ChallengeResult { get; set; }
    public required long DurationMs { get; set; }
}

public sealed class PracticeModeReportChallengeDetail
{
    public required SimpleEntity Spec { get; set; }
    public required SimpleEntity Game { get; set; }
    public required IEnumerable<PracticeModeReportChallengeDetailUser> Users { get; set; }
    public required PagingResults Paging { get; set; }
}

public sealed class PracticeModeReportChallengeDetailUser
{
    public required PlayerWithSponsor User { get; set; }
    public required SimpleSponsor Sponsor { get; set; }
    public required int AttemptCount { get; set; }
    public required DateTimeOffset LastAttemptDate { get; set; }
    public required DateTimeOffset BestAttemptDate { get; set; }
    public required double BestAttemptDurationMs { get; set; }
    public required ChallengeResult BestAttemptResult { get; set; }
    public required double BestAttemptScore { get; set; }
}
