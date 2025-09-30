// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Gameboard.Api.Features.Teams;

public interface ICumulativeTimeCalculator
{
    long CalculativeCumulativeTimeMs(IEnumerable<TeamChallengeTime> times);
}

public class CumulativeTimeCalculator : ICumulativeTimeCalculator
{
    public long CalculativeCumulativeTimeMs(IEnumerable<TeamChallengeTime> times)
        => times
            .Where(t => t.StartTime.IsNotEmpty())
            .Where(t => t.LastScoreTime.IsNotEmpty())
            .Sum(t => Math.Max(t.LastScoreTime.Value.ToUnixTimeMilliseconds() - t.StartTime.Value.ToUnixTimeMilliseconds(), 0));
}
