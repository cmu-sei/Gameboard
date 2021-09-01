// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api
{
    public class SponsorReport
    {
        public string Title { get; set; } = "SponsorReport";
        public DateTimeOffset Timestamp { get; set; }
        public SponsorStat[] Stats { get; set; }
    }

    public class SponsorStat
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Logo { get; set; }
        public int Count { get; set; }
    }

    public class ChallengeReport
    {
        public string Title { get; set; } = "ChallengeReport";
        public DateTimeOffset Timestamp { get; set; }
        public ChallengeStat[] Stats { get; set; }
    }
    public class ChallengeStat
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public int SuccessCount { get; set; }
        public int PartialCount { get; set; }

    }

}
