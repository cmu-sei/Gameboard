using System;
using System.Collections.Generic;
using Gameboard.Api.Common;

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
    public required SimpleEntity User { get; set; }
    public required EnrollmentReportPlayerViewModel Player { get; set; }
    public required EnrollmentReportGameViewModel Game { get; set; }
    public required EnrollmentReportPlayTimeViewModel PlayTime { get; set; }
    public required EnrollmentReportTeamViewModel Team { get; set; }

    // performance data
    public required IEnumerable<EnrollmentReportChallengeViewModel> Challenges { get; set; }
    public required int ChallengesPartiallySolvedCount { get; set; }
    public required int ChallengesCompletelySolvedCount { get; set; }
    public required double Score { get; set; }
}

public class EnrollmentReportPlayerViewModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset? EnrollDate { get; set; }
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

public class EnrollmentReportPlayTimeViewModel
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
    public required IEnumerable<EnrollmentReportManualChallengeBonus> ManualChallengeBonuses { get; set; }
    public required double? Score { get; set; }
    public required double? MaxPossiblePoints { get; set; }
    public required ChallengeResult Result { get; set; }
}

public class EnrollmentReportManualChallengeBonus
{
    public required string Description { get; set; }
    public required double Points { get; set; }
}

public class EnrollmentReportSponsorViewModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string LogoFileName { get; set; }
}

// this class isn't sent down with the report data, it's just used as an intermediate
// while querying to minimize the amount we pull back from the db
internal class EnrollmentReportChallengeQueryData
{
    public required string SpecId { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset WhenCreated { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
    public required IEnumerable<EnrollmentReportManualChallengeBonus> ManualChallengeBonuses { get; set; }
    public required double? Score { get; set; }
    public required double MaxPossiblePoints { get; set; }
}

public class EnrollmentReportCsvRecord
{
    // user
    public required string UserId { get; set; }
    public required string UserName { get; set; }

    // player
    public required string PlayerId { get; set; }
    public required string PlayerName { get; set; }
    public required DateTimeOffset? PlayerEnrollDate { get; set; }
    public required string PlayerSponsor { get; set; }

    // game
    public required string GameId { get; set; }
    public required string GameName { get; set; }
    public required bool IsTeamGame { get; set; }
    public required string Series { get; set; }
    public required string Season { get; set; }
    public required string Track { get; set; }

    // session
    public required DateTimeOffset? PlayStart { get; set; }
    public required DateTimeOffset? PlayEnd { get; set; }
    public required decimal? PlayDurationInSeconds { get; set; }

    // team data
    public required string TeamId { get; set; }
    public required string TeamName { get; set; }
    public required string CaptainPlayerId { get; set; }
    public required string CaptainPlayerName { get; set; }
    public required string TeamSponsors { get; set; }

    // challenges (denormalizing fields)
    public required string Challenges { get; set; }
    public required DateTimeOffset? FirstDeployDate { get; set; }
    public required DateTimeOffset? FirstStartDate { get; set; }
    public required DateTimeOffset? LastEndDate { get; set; }
    public required double? MinDurationInSeconds { get; set; }
    public required double? MaxDurationInSeconds { get; set; }
    public required string ChallengeScores { get; set; }

    // challenge / game performance summary
    public required int ChallengesPartiallySolvedCount { get; set; }
    public required int ChallengesCompletelySolvedCount { get; set; }
    public required double Score { get; set; }
}

public sealed class EnrollmentReportLineChartGroup
{
    public required IEnumerable<EnrollmentReportLineChartPlayer> Players { get; set; }
    public required int TotalCount { get; set; }
}

public sealed class EnrollmentReportLineChartPlayer
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required DateTimeOffset EnrollDate { get; set; }
    public required SimpleEntity Game { get; set; }
}
