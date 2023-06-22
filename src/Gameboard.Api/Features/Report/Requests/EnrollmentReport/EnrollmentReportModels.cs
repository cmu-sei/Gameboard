using System;
using System.Collections.Generic;
using Gameboard.Api.Features.Common;

namespace Gameboard.Api.Features.Reports;

public class EnrollmentReportParameters
{
    public DateTimeOffset? EnrollDateStart { get; set; }
    public DateTimeOffset? EnrollDateEnd { get; set; }
    public string Seasons { get; set; }
    public string Series { get; set; }
    public string Sponsors { get; set; }
    public string Tracks { get; set; }
}

public class EnrollmentReportRecord
{
    public required EnrollmentReportPlayerViewModel Player { get; set; }
    public required EnrollmentReportGameViewModel Game { get; set; }
    public required EnrollmentReportSessionViewModel Session { get; set; }
    public required double Score { get; set; }

    // team data
    public required EnrollmentReportTeamViewModel Team { get; set; }

    // challenge data
    public required IEnumerable<EnrollmentReportChallengeViewModel> Challenges { get; set; }
    public required int ChallengesPartiallySolvedCount { get; set; }
    public required int ChallengesCompletelySolvedCount { get; set; }
}

public class EnrollmentReportPlayerViewModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required EnrollmentReportSponsorViewModel Sponsor { get; set; }
}

public class EnrollmentReportGameViewModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsTeamGame { get; set; }
    public required string Series { get; set; }
    public required string Season { get; set; }
    public required string Track { get; set; }
}

public class EnrollmentReportSessionViewModel
{
    public required DateTimeOffset? Start { get; set; }
    public required DateTimeOffset? End { get; set; }
    public required double? DurationMs { get; set; }
}

public class EnrollmentReportTeamViewModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required SimpleEntity CurrentCaptain { get; set; }
    public required IEnumerable<EnrollmentReportSponsorViewModel> Sponsors { get; set; }
}

public class EnrollmentReportChallengeViewModel
{
    public required string Name { get; set; }
    public required string SpecId { get; set; }
    public required DateTimeOffset? DeployDate { get; set; }
    public required DateTimeOffset? StartDate { get; set; }
    public required DateTimeOffset? EndDate { get; set; }
    public required double? DurationMs { get; set; }
    public required double? Score { get; set; }
    public required double? MaxPossiblePoints { get; set; }
    public required ChallengeResult Result { get; set; }
}

public class EnrollmentReportSponsorViewModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string LogoFileName { get; set; }
}

// this class isn't sent down with the report data, it's just used as an intermediate
// while querying to minimize the amount we pull back from the db
public class EnrollmentReportChallengeQueryData
{
    public required string SpecId { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset WhenCreated { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
    public required double? Score { get; set; }
    public required double MaxPossiblePoints { get; set; }
}
