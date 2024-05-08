using System;
using System.Collections.Generic;

namespace Gameboard.Api.Features.Reports;

public sealed class SiteUsageReportParameters
{
    public string Sponsors { get; set; }
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
    public required int DeployedChallengesSpecCount { get; set; }
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
    public required string Name { get; set; }
    public required int DeployCount { get; set; }
    public required int SolveCompleteCount { get; set; }
    public required int SolvePartialCount { get; set; }
    public required IEnumerable<SimpleEntity> UsedInGames { get; set; }
}

public sealed class SiteUsageReportPlayer
{
    public required int ChallengeCountCompetitive { get; set; }
    public required int ChallengeCountPractice { get; set; }
    public required DateTimeOffset LastActive { get; set; }
    public required string Name { get; set; }
    public required SimpleSponsor Sponsor { get; set; }
    public required string UserId { get; set; }
}

public sealed class SiteUsageReportPlayersParameters
{
    public required PlayerMode? ExclusiveToMode { get; set; }
}

public sealed class SiteUsageReportSponsor
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Logo { get; set; }
    public required string ParentName { get; set; }
    public required int PlayerCount { get; set; }
}
