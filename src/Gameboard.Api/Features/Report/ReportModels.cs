// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api;

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
    public int SessionPlayerCount { get; set; }
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
    public string AverageTime { get; set; }
    public int AttemptCount { get; set; }
    public int AverageScore { get; set; }
}

public class ChallengeDetailReport
{
    public string Title { get; set; } = "Challenge Detail Report";
    public DateTime Timestamp { get; set; }
    public Part[] Parts { get; set; }
    public int AttemptCount { get; set; }
    public string ChallengeId { get; set; }
}

#region Ticket Reports
public class TicketDetail
{
    public int Key { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public string Challenge { get; set; }
    public string GameSession { get; set; }
    public string Team { get; set; }
    public string Assignee { get; set; }
    public string Requester { get; set; }
    public string Creator { get; set; }
    public DateTimeOffset Created { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public string Label { get; set; }
    public string Status { get; set; }
}

/*public class TicketDetailReport
{
    public string Title { get; set; } = "Ticket Detail Report";
    public DateTime Timestamp { get; set; }
    public TicketDetail[] Details { get; set; }
}*/
#endregion

public class ParticipationReportV1
{
    public string Key { get; set; } = "Participation";
    public DateTime Timestamp { get; set; }
    public ParticipationStat[] Stats { get; set; }
}

public class ParticipationStat
{
    public string Key { get; set; }
    public int GameCount { get; set; }
    public int PlayerCount { get; set; }
    public int SessionPlayerCount { get; set; }
    public int TeamCount { get; set; }
    public int SessionTeamCount { get; set; }
    public int ChallengesDeployedCount { get; set; }
}

public class SeriesReport : ParticipationReportV1
{
    public SeriesReport()
    {
        Key = "Series";
    }
}

public class TrackReport : ParticipationReportV1
{
    public TrackReport()
    {
        Key = "Track";
    }
}

public class SeasonReport : ParticipationReportV1
{
    public SeasonReport()
    {
        Key = "Season";
    }
}

public class DivisionReport : ParticipationReportV1
{
    public DivisionReport()
    {
        Key = "Division";
    }
}

public class ModeReport : ParticipationReportV1
{
    public ModeReport()
    {
        Key = "Mode";
    }
}

public class CorrelationReport
{
    public string Title { get; set; } = "Correlation Report";
    public DateTime Timestamp { get; set; }
    public CorrelationStat[] Stats { get; set; }
}

public class CorrelationStat
{
    public int GameCount { get; set; }
    public int UserCount { get; set; }
}

public class Part
{
    public string Text { get; set; }
    public int SolveCount { get; set; }
    public int AttemptCount { get; set; }
    public float Weight { get; set; }
}

public class ChallengeStatsExport
{
    public string GameName { get; set; }
    public string ChallengeName { get; set; }
    public string Tag { get; set; }
    public string Points { get; set; }
    public string Attempts { get; set; }
    public string Complete { get; set; }
    public string Partial { get; set; }
    public string AvgTime { get; set; }
    public string AvgScore { get; set; }
}

public class ChallengeDetailsExport
{
    public string GameName { get; set; }
    public string ChallengeName { get; set; }
    public string Tag { get; set; }
    public string Question { get; set; }
    public string Points { get; set; }
    public string Solves { get; set; }
}

public class TicketDetailsExport
{
    public string Key { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public string Challenge { get; set; }
    public string GameSession { get; set; }
    public string Team { get; set; }
    public string Assignee { get; set; }
    public string Requester { get; set; }
    public string Creator { get; set; }
    public string Created { get; set; }
    public string LastUpdated { get; set; }
    public string Label { get; set; }
    public string Status { get; set; }
}

public interface IReport
{
    public ReportMetaData MetaData { get; set; }
}

public sealed class ReportMetaData
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public required DateTimeOffset RunAt { get; set; }
}

public class ParticipationReportArgs
{
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string SponsorId { get; set; }
}

public class ParticipationReportRecord
{
    public Sponsor Sponsor { get; set; }
    public SimpleEntity User { get; set; }
    public SimpleEntity Game { get; set; }
    public SimpleEntity Challenge { get; set; }
    public double CorrectCount { get; set; }
    public double PartialCount { get; set; }
}

public class ParticipationReport : IReport
{
    public required ReportMetaData MetaData { get; set; }
    public required IEnumerable<ParticipationReportRecord> Records { get; set; }
}
