using System;
using Gameboard.Api.Common;

namespace Gameboard.Api.Features.Reports;

public class GetChallengesReportQueryArgs
{
    public string ChallengeSpecId { get; set; }
    public string Competition { get; set; }
    public string GameId { get; set; }
    public DateTimeOffset RegistrationStart { get; set; }
    public DateTimeOffset RegistrationEnd { get; set; }
    public string TrackName { get; set; }
}

public class PlayerTime
{
    public required SimpleEntity Player { get; set; }
    public TimeSpan Time { get; set; }
}

internal class ChallengesReportPlayerEngagement
{
    public required SimpleEntity Player { get; set; }
    public required SimpleEntity Game { get; set; }
    public required SimpleEntity Challenge { get; set; }
    public required bool IsRegisteredForGame { get; set; }
    public required bool IsChallengeStarted { get; set; }
    public required bool IsChallengeDeployed { get; set; }
}

internal class ChallengesReportMeanChallengeStats
{
    public required double? MeanCompleteSolveTimeMs { get; set; }
    public required double? MeanScore { get; set; }
}

internal class ChallengesReportPlayer
{
    public required SimpleEntity Player { get; set; }
    public required Nullable<double> SolveTimeMs { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
    public required ChallengeResult Result { get; set; }
    public required int Score { get; set; }
}

internal class ChallengesReportChallenge
{
    public required SimpleEntity Challenge { get; set; }
    public required SimpleEntity Game { get; set; }
    public required string SpecId { get; set; }
    public required ChallengesReportPlayer Player { get; set; }
    public required int TicketCount { get; set; }
}

internal class ChallengesReportSpec
{
    public required string Id { get; set; }
    public required SimpleEntity Game { get; set; }
    public required string Name { get; set; }
    public required double MaxPoints { get; set; }
}

public class ChallengesReportPlayerSolve
{
    public required SimpleEntity Player { get; set; }
    public required double SolveTimeMs { get; set; }
}

public class ChallengesReportRecord
{
    public required SimpleEntity ChallengeSpec { get; set; }
    public required SimpleEntity Challenge { get; set; }
    public required SimpleEntity Game { get; set; }
    public required int PlayersEligible { get; set; }
    public required int PlayersStarted { get; set; }
    public required int PlayersWithPartialSolve { get; set; }
    public required int PlayersWithCompleteSolve { get; set; }
    public required ChallengesReportPlayerSolve FastestSolve { get; set; }
    public required double MaxPossibleScore { get; set; }
    public required double? MeanScore { get; set; }
    public required double? MeanCompleteSolveTimeMs { get; set; }
    public required int TicketCount { get; set; }
}

public class ChallengesReportCsvRecord
{
    public required string ChallengeSpecId { get; set; }
    public required string ChallengeSpecName { get; set; }
    public required string GameId { get; set; }
    public required string GameName { get; set; }
    public required int PlayersEligible { get; set; }
    public required int PlayersStarted { get; set; }
    public required int PlayersWithPartialSolve { get; set; }
    public required int PlayersWithCompleteSolve { get; set; }
    public required string FastestSolvePlayerId { get; set; }
    public required string FastestSolvePlayerName { get; set; }
    public required string FastestSolveTimeMs { get; set; }
    public required double MaxPossibleScore { get; set; }
    public required double? MeanScore { get; set; }
    public required double? MeanCompleteSolveTimeMs { get; set; }
    public required int TicketCount { get; set; }
}

