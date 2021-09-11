// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api
{
    public class SponsorReport
    {
        public string Title { get; set; } = "Sponsor Report";
        public DateTime Timestamp { get; set; }
        public SponsorStat[] Stats { get; set; }
    }

    public class GameSponsorReport
    {
        public string Title { get; set; } = "Game Sponsor Report";
        public DateTime Timestamp { get; set; }
        public GameSponsorStat[] Stats { get; set; }
    }

    public class UserReport
    {
        public string Title { get; set; } = "User Report";
        public DateTime Timestamp { get; set; }
        public int EnrolledUserCount { get; set; }
        public int UnenrolledUserCount { get; set; }
    }

    public class PlayerReport
    {
        public string Title { get; set; } = "Player Report";
        public DateTime Timestamp { get; set; }
        public PlayerStat[] Stats { get; set; }
    }

    public class PlayerStat
    {
        public string GameId { get; set; }
        public string GameName { get; set; }
        public int PlayerCount { get; set; }
    }

    public class SponsorStat
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Logo { get; set; }
        public int Count { get; set; }
        public int TeamCount { get; set; }
    }

    public class GameSponsorStat
    {
        public string GameId { get; set; }
        public string GameName { get; set; }
        public SponsorStat[] Stats { get; set; }
    }

    public class ChallengeReport
    {
        public string Title { get; set; } = "Challenge Report";
        public DateTimeOffset Timestamp { get; set; }
        public ChallengeStat[] Stats { get; set; }
    }

    public class ChallengeStat
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Tag { get; set; }
        public int Points { get; set; }
        public int SuccessCount { get; set; }
        public int PartialCount { get; set; }
        public int FailureCount { get; set; }
        public string AverageTime { get; set; }
        public int AttemptCount { get; set; }
    }
}
