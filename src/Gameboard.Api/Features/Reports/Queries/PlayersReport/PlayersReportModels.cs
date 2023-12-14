using System;

namespace Gameboard.Api.Features.Reports;

public sealed class PlayersReportParameters
{
    public DateTimeOffset? CreatedDateStart { get; set; }
    public DateTimeOffset? CreatedDateEnd { get; set; }
    public DateTimeOffset? LastPlayedDateStart { get; set; }
    public DateTimeOffset? LastPlayedDateEnd { get; set; }
    public string Sponsors { get; set; }
}

public sealed class PlayersReportRecord
{
    public required SimpleEntity User { get; set; }
    public required ReportSponsorViewModel Sponsor { get; set; }
    public required DateTimeOffset CreatedOn { get; set; }
    public required DateTimeOffset? LastPlayedOn { get; set; }
    public required int DeployedPracticeChallengesCount { get; set; }
    public required int DeployedCompetitiveChallengesCount { get; set; }
    public required int DistinctGamesPlayedCount { get; set; }
    public required int DistinctSeriesPlayedCount { get; set; }
    public required int DistinctTracksPlayedCount { get; set; }
    public required int DistinctSeasonsPlayedCount { get; set; }
}
