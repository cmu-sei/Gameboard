using System;
using System.Collections.Generic;
using Gameboard.Api.Structure;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public class GetChallengesReportQueryArgs
{
    public string ChallengeSpecId { get; set; }
    public string Competition { get; set; }
    public string GameId { get; set; }
    public string Track { get; set; }
}

public class PlayerTime
{
    public required SimpleEntity Player { get; set; }
    public TimeSpan Time { get; set; }
}

internal class ChallengeReportPlayerEngagement
{
    public required SimpleEntity Player { get; set; }
    public required SimpleEntity Game { get; set; }
    public required SimpleEntity Challenge { get; set; }
    public required bool IsRegisteredForGame { get; set; }
    public required bool IsChallengeStarted { get; set; }
    public required bool IsChallengeDeployed { get; set; }
}

internal class ChallengeReportMeanChallengeStats
{
    public required Nullable<double> MeanCompleteSolveTimeMs { get; set; }
    public required Nullable<double> MeanScore { get; set; }
}

internal class ChallengeReportPlayer
{
    public required SimpleEntity Player { get; set; }
    public required Nullable<double> SolveTimeMs { get; set; }
    public required DateTimeOffset StartTime { get; set; }
    public required DateTimeOffset EndTime { get; set; }
    public required ChallengeResult Result { get; set; }
    public required int Score { get; set; }
}

internal class ChallengeReportChallenge
{
    public required SimpleEntity Challenge { get; set; }
    public required SimpleEntity Game { get; set; }
    public required string SpecId { get; set; }
    public required ChallengeReportPlayer Player { get; set; }
}

internal class ChallengeReportSpec
{
    public required string Id { get; set; }
    public required SimpleEntity Game { get; set; }
    public required string Name { get; set; }
    public required double MaxPoints { get; set; }
}

public class ChallengeReportPlayerSolve
{
    public required SimpleEntity Player { get; set; }
    public required double SolveTimeMs { get; set; }
}

public class ChallengeReportRecord
{
    public required SimpleEntity ChallengeSpec { get; set; }
    public required SimpleEntity Game { get; set; }
    public required int PlayersEligible { get; set; }
    public required int PlayersStarted { get; set; }
    public required int PlayersWithPartialSolve { get; set; }
    public required int PlayersWithCompleteSolve { get; set; }
    public required ChallengeReportPlayerSolve FastestSolve { get; set; }
    public required double MaxPossibleScore { get; set; }
    public required double? MeanScore { get; set; }
    public required double? MeanCompleteSolveTimeMs { get; set; }
}

public class ChallengesReportResults : IReportResult<ChallengeReportRecord>
{
    public required ReportMetaData MetaData { get; set; }
    public required IEnumerable<ChallengeReportRecord> Records { get; set; }
}

public record GetChallengeReportQuery(GetChallengesReportQueryArgs Args) : IRequest<ChallengesReportResults>;
