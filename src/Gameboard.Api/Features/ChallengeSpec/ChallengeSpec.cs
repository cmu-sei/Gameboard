// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using Gameboard.Api.Features.GameEngine;

namespace Gameboard.Api;

public class ExternalSpec
{
    public string ExternalId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Text { get; set; }
    public GameEngineType GameEngineType { get; set; }
    public string SolutionGuideUrl { get; set; }
    public bool ShowSolutionGuideInCompetitiveMode { get; set; }
    public string Tags { get; set; }
}

public class SpecDetail : ExternalSpec
{
    public string Tag { get; set; }
    public bool Disabled { get; set; }
    public int AverageDeploySeconds { get; set; }
    public bool IsHidden { get; set; }
    public int Points { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float R { get; set; }
}

public class ChallengeSpec : SpecDetail
{
    public string Id { get; set; }
    public string GameId { get; set; }
}

public class GameChallengeSpecs
{
    public required SimpleEntity Game { get; set; }
    public required IEnumerable<SimpleEntity> ChallengeSpecs { get; set; }
}

public class NewChallengeSpec : SpecDetail
{
    public string GameId { get; set; }
}

public class ChangedChallengeSpec : SpecDetail
{
    public string Id { get; set; }
}

public class BoardSpec
{
    public string Id { get; set; }
    public string Tag { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Disabled { get; set; }
    public int AverageDeploySeconds { get; set; }
    public int Points { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float R { get; set; }
}

public sealed class ChallengeSpecSummary
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Text { get; set; }
    public required string GameId { get; set; }
    public required string GameName { get; set; }
    public required string GameLogo { get; set; }
    public required int AverageDeploySeconds { get; set; }
    public required string SolutionGuideUrl { get; set; }
    public required bool ShowSolutionGuideInCompetitiveMode { get; set; }
    public required IEnumerable<string> Tags { get; set; }
}

public sealed class ChallengeSpecQuestionPerformance
{
    public required int QuestionRank { get; set; }
    public required string Hint { get; set; }
    public required string Prompt { get; set; }
    public required double PointValue { get; set; }

    public required int CountCorrect { get; set; }
    public required int CountSubmitted { get; set; }
}

public sealed class ChallengeSpecQuestionPerformanceChallenge
{
    public required bool IsComplete { get; set; }
    public required bool IsPartial { get; set; }
    public required bool IsZero { get; set; }
    public required GameEngineGameState State { get; set; }
}
