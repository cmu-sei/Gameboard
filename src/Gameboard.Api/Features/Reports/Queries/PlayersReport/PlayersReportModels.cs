using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Reports;

public sealed class PlayersReportParameters
{
    public DateTimeOffset? CreatedDateStart { get; set; }
    public DateTimeOffset? CreatedDateEnd { get; set; }
    public string Games { get; set; }
    public DateTimeOffset? LastPlayedDateStart { get; set; }
    public DateTimeOffset? LastPlayedDateEnd { get; set; }
    public string Seasons { get; set; }
    public string Series { get; set; }
    public string Sponsors { get; set; }
    public string Tracks { get; set; }
    public string Sort { get; set; }
    public SortDirection SortDirection { get; set; }
}

public sealed class PlayersReportRecord
{
    public required SimpleEntity User { get; set; }
    public required ReportSponsorViewModel Sponsor { get; set; }
    public required DateTimeOffset CreatedOn { get; set; }
    public required DateTimeOffset? LastPlayedOn { get; set; }
    public required int CompletedCompetitiveChallengesCount { get; set; }
    public required int CompletedPracticeChallengesCount { get; set; }
    public required int DeployedCompetitiveChallengesCount { get; set; }
    public required int DeployedPracticeChallengesCount { get; set; }
    public required IEnumerable<string> DistinctGamesPlayed { get; set; }
    public required IEnumerable<string> DistinctSeriesPlayed { get; set; }
    public required IEnumerable<string> DistinctTracksPlayed { get; set; }
    public required IEnumerable<string> DistinctSeasonsPlayed { get; set; }
}

public sealed class PlayersReportCsvRecord
{
    public required string UserId { get; set; }
    public required string UserName { get; set; }
    public required string SponsorId { get; set; }
    public required string SponsorName { get; set; }
    public required DateTimeOffset? CreatedOn { get; set; }
    public required DateTimeOffset? LastPlayedOn { get; set; }
    public required int CompletedCompetitiveChallengesCount { get; set; }
    public required int CompletedPracticeChallengesCount { get; set; }
    public required int DeployedPracticeChallengesCount { get; set; }
    public required int DeployedCompetitiveChallengesCount { get; set; }
    public required int DistinctGamesPlayedCount { get; set; }
    public required int DistinctSeriesPlayedCount { get; set; }
    public required int DistinctTracksPlayedCount { get; set; }
    public required int DistinctSeasonsPlayedCount { get; set; }
}

public sealed class PlayersReportStatSummary
{
    public required int UserCount { get; set; }
    public required int UsersWithCompletedCompetitiveChallengeCount { get; set; }
    public required int UsersWithDeployedCompetitiveChallengeCount { get; set; }
    public required int UsersWithCompletedPracticeChallengeCount { get; set; }
    public required int UsersWithDeployedPracticeChallengeCount { get; set; }
}
