using System.Collections.Generic;
using Gameboard.Api.Common;

namespace Gameboard.Api.Features.Reports;

public sealed class PlayersReportTrackModifier
{
    public static string CompetedInThisTrack { get => "include"; }
    public static string CompetedInOnlyThisTrack { get => "include-only"; }
    public static string DidntCompeteInThisTrack { get => "exclude"; }
}

public sealed class PlayersReportQueryParameters
{
    public string ChallengeSpecId { get; set; }
    public string Series { get; set; }
    public string GameId { get; set; }
    public string SponsorId { get; set; }
    public ReportDateRange SessionStartWindow { get; set; }
    public string TrackName { get; set; }
    public string TrackModifier { get; set; }
}

public sealed class PlayersReportSponsor
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string LogoUri { get; set; }
}

public sealed class PlayersReportGamesAndChallengesSummary
{
    public required IEnumerable<SimpleEntity> Deployed { get; set; }
    public required IEnumerable<SimpleEntity> Enrolled { get; set; }
    public required IEnumerable<SimpleEntity> ScoredPartial { get; set; }
    public required IEnumerable<SimpleEntity> ScoredComplete { get; set; }
}

public sealed class PlayersReportRecord
{
    public required SimpleEntity User { get; set; }
    public required IEnumerable<PlayersReportSponsor> Sponsors { get; set; }
    public required PlayersReportGamesAndChallengesSummary Challenges { get; set; }
    public required PlayersReportGamesAndChallengesSummary Games { get; set; }
    public required IEnumerable<string> TracksPlayed { get; set; }
    public required IEnumerable<string> CompetitionsPlayed { get; set; }
}

public sealed class PlayersReportCsvRecordChallenge
{
    public required string ChallengeId { get; set; }
    public required string ChallengeName { get; set; }
    public required double ChallengeScore { get; set; }
}

public sealed class PlayersReportExportRecord
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string SponsorName { get; set; }
    public required string Competition { get; set; }
    public required string Track { get; set; }
    public required string GameId { get; set; }
    public required string GameName { get; set; }
    public required string ChallengeSummary { get; set; }
    public required string PlayerId { get; set; }
    public required string PlayerName { get; set; }
    public required double MaxPossibleScore { get; set; }
    public required double Score { get; set; }
}