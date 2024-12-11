using System;
using System.Collections.Generic;
using Gameboard.Api.Features.Feedback;

namespace Gameboard.Api.Features.Reports;

public sealed class FeedbackReportParameters
{
    public required string TemplateId { get; set; }

    public string Games { get; set; }
    public string Seasons { get; set; }
    public string Sponsors { get; set; }
    public string Series { get; set; }
    public DateTimeOffset? SubmissionDateStart { get; set; }
    public DateTimeOffset? SubmissionDateEnd { get; set; }
    public string Tracks { get; set; }

    public string Sort { get; set; }
    public SortDirection SortDirection { get; set; }
}

public sealed class FeedbackReportSummaryData
{
    public required int? QuestionCount { get; set; }
    public required int ResponseCount { get; set; }
    public required int UnfinalizedCount { get; set; }
    public required int UniqueChallengesCount { get; set; }
    public required int UniqueGamesCount { get; set; }
}

public sealed class FeedbackReportRecord
{
    public required string Id { get; set; }
    public required SimpleEntity ChallengeSpec { get; set; }
    public required FeedbackReportRecordGame LogicalGame { get; set; }
    public required IEnumerable<QuestionSubmission> Responses { get; set; }
    public required ReportSponsorViewModel Sponsor { get; set; }
    public required SimpleEntity User { get; set; }
    public required FeedbackSubmissionAttachedEntity Entity { get; set; }
    public required DateTimeOffset WhenCreated { get; set; }
    public required DateTimeOffset? WhenEdited { get; set; }
    public required DateTimeOffset? WhenFinalized { get; set; }
}

public sealed class FeedbackReportExportRecord
{
    public required string Id { get; set; }
    public required string ChallengeSpecId { get; set; }
    public required string ChallengeSpecName { get; set; }
    public required string GameId { get; set; }
    public required string GameName { get; set; }
    public required string GameSeason { get; set; }
    public required string GameSeries { get; set; }
    public required string GameTrack { get; set; }
    public required bool IsTeamGame { get; set; }
    public required string SponsorId { get; set; }
    public required string SponsorName { get; set; }
    public required string UserId { get; set; }
    public required string UserName { get; set; }
    public required DateTimeOffset WhenCreated { get; set; }
    public required DateTimeOffset? WhenEdited { get; set; }
    public required DateTimeOffset? WhenFinalized { get; set; }
}

public sealed class FeedbackReportRecordGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Division { get; set; }
    public required string Season { get; set; }
    public required string Series { get; set; }
    public required string Track { get; set; }
    public required bool IsTeamGame { get; set; }
}
