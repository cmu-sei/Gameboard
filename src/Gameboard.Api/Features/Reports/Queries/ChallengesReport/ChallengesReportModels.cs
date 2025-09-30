// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Reports;

namespace Gameboard.Api.Features.Challenges;

public sealed class ChallengesReportParameters
{
    public required DateTimeOffset? StartDateStart { get; set; }
    public required DateTimeOffset? StartDateEnd { get; set; }
    public required string Games { get; set; }
    public required string Seasons { get; set; }
    public required string Series { get; set; }
    public string Sort { get; set; }
    public SortDirection SortDirection { get; set; }
    public required string Tags { get; set; }
    public required string Tracks { get; set; }
}

public sealed class ChallengesReportRecord
{
    public required SimpleEntity ChallengeSpec { get; set; }
    public required ReportGameViewModel Game { get; set; }
    public required PlayerMode PlayerModeCurrent { get; set; }
    public required int Points { get; set; }
    public required IEnumerable<string> Tags { get; set; }

    // aggregations
    public required double? AvgScore { get; set; }
    public required double? AvgCompleteSolveTimeMs { get; set; }
    public required int DeployCompetitiveCount { get; set; }
    public required int DeployPracticeCount { get; set; }
    public required int DistinctPlayerCount { get; set; }
    public required int PracticeSolveZeroCount { get; set; }
    public required int PracticeSolvePartialCount { get; set; }
    public required int PracticeSolveCompleteCount { get; set; }
    public required int SolveZeroCount { get; set; }
    public required int SolvePartialCount { get; set; }
    public required int SolveCompleteCount { get; set; }
}

public sealed class ChallengesReportStatSummary
{
    public required int DeployCompetitiveCount { get; set; }
    public required int DeployPracticeCount { get; set; }
    public required int SpecCount { get; set; }
    public required ChallengesReportStatSummaryPopularChallenge MostPopularCompetitiveChallenge { get; set; }
    public required ChallengesReportStatSummaryPopularChallenge MostPopularPracticeChallenge { get; set; }
}

public sealed class ChallengesReportStatSummaryPopularChallenge
{
    public required int DeployCount { get; set; }
    public required string ChallengeName { get; set; }
    public required string GameName { get; set; }
}

public sealed class ChallengesReportExportRecord
{
    public required string ChallengeSpecId { get; set; }
    public required string ChallengeSpecName { get; set; }
    public required string GameId { get; set; }
    public required string GameName { get; set; }
    public required string GameSeason { get; set; }
    public required string GameSeries { get; set; }
    public required string GameTrack { get; set; }
    public required PlayerMode CurrentPlayerMode { get; set; }
    public required int Points { get; set; }
    public required string GameEngineTags { get; set; }
    public required double? AvgCompleteSolveTimeMs { get; set; }
    public required string AvgCompleteSolveTime { get; set; }
    public required double? AvgScore { get; set; }
    public required int DeployCompetitiveCount { get; set; }
    public required int DeployPracticeCount { get; set; }
    public required int DistinctPlayerCount { get; set; }

    // these solve counts are for competitive
    public required int SolveZeroCount { get; set; }
    public required int SolvePartialCount { get; set; }
    public required int SolveCompleteCount { get; set; }
    public required double? SolveZeroPct { get; set; }
    public required double? SolvePartialPct { get; set; }
    public required double? SolveCompletePct { get; set; }

    // these are for practice
    public required int PracticeSolveZeroCount { get; set; }
    public required int PracticeSolvePartialCount { get; set; }
    public required int PracticeSolveCompleteCount { get; set; }
    public required double? PracticeSolveZeroPct { get; set; }
    public required double? PracticeSolvePartialPct { get; set; }
    public required double? PracticeSolveCompletePct { get; set; }
}
