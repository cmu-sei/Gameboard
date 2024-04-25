using System;

namespace Gameboard.Api.Features.Reports;

public sealed class SiteUsageReportParameters
{
    public string SponsorId { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}

public sealed class SiteUsageReportRecord
{
    public required double? AvgCompetitiveChallengesPerCompetitiveUser { get; set; }
    public required double? AvgPracticeChallengesPerPracticeUser { get; set; }
    public required int DeployedChallengesCount { get; set; }
    public required int DeployedChallengesCompetitiveCount { get; set; }
    public required int DeployedChallengesPracticeCount { get; set; }
    public required int DeployedChallengeSpecCount { get; set; }
    public required int UsersWithCompetitiveChallengeCount { get; set; }
    public required int UsersWithPracticeChallengeCount { get; set; }
    public required int CompetitiveUsersWithNoPracticeCount { get; set; }
    public required int PracticeUsersWithNoCompetitiveCount { get; set; }
    public required int SponsorCount { get; set; }
    public required int UserCount { get; set; }
}

public sealed class SiteUsageReportChallenge
{
    public required string Id { get; set; }
    public required DateTimeOffset DeployedDate { get; set; }
    public required bool IsCompetitive { get; set; }
    public required string SpecId { get; set; }
    public required string TeamId { get; set; }
}
