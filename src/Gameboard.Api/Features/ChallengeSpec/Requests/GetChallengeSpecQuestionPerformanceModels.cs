// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Gameboard.Api.Features.ChallengeSpecs;

public sealed class GetChallengeSpecQuestionPerformanceResult
{
    public required SimpleEntity ChallengeSpec { get; set; }
    public required double MaxPossibleScore { get; set; }
    public required SimpleEntity Game { get; set; }
    public required IEnumerable<ChallengeSpecQuestionPerformance> Questions { get; set; }
}
