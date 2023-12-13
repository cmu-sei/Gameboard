using System;

namespace Gameboard.Api.Features.Reports;

public sealed class PlayersReportParameters
{
    public DateTimeOffset? CreatedDateStart { get; set; }
    public DateTimeOffset? CreatedDateEnd { get; set; }
    public DateTimeOffset? LastPlayedDateStart { get; set; }
    public DateTimeOffset? LastPlayedDateEnd { get; set; }
    public int? DeployedCompetitiveChallengeCount { get; set; }
    public int? DeployedPracticeChallengeCount { get; set; }
    public string SponsorId { get; set; }
}

public sealed class PlayersReportRecord
{
    public SimpleEntity User { get; set; }
    public ReportSponsorViewModel Sponsor { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? LastPlayedOn { get; set; }
    public int DeployedPracticeChallenges { get; set; }
    public int DeployedCompetitiveChallenges { get; set; }
}
