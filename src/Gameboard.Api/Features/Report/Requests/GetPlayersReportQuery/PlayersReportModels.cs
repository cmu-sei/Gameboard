using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Reports;

public sealed class PlayersReportQueryParameters
{
    public string ChallengeSpecId { get; set; }
    public string Competition { get; set; }
    public string GameId { get; set; }
    public string SponsorId { get; set; }
    public ReportDateRange SessionStartWindow { get; set; }
    public string Track { get; set; }
}

public sealed class PlayersReportSponsor
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string LogoUri { get; set; }
}

public sealed class PlayersReportGamesAndChallengesSummary
{
    public required int CountEnrolled { get; set; }
    public required int CountDeployed { get; set; }
    public required int CountScoredPartial { get; set; }
    public required int CountScoredComplete { get; set; }
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

public sealed class PlayersReportCsvRecord
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string SponsorName { get; set; }
    public required string Competition { get; set; }
    public required string Track { get; set; }
    public required string GameId { get; set; }
    public required string GameName { get; set; }
    public required IEnumerable<PlayersReportCsvRecordChallenge> Challenges { get; set; }
    public required string PlayerId { get; set; }
    public required string PlayerName { get; set; }
    public required double MaxPossibleScore { get; set; }
    public required double Score { get; set; }
}

public sealed class PlayersReportResults : IReportResult<PlayersReportRecord>
{
    public required ReportMetaData MetaData { get; set; }
    public required IEnumerable<PlayersReportRecord> Records { get; set; }
}
