// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Feedback;

public class FeedbackSubmission
{
    // UserId and PlayerId are set automatically when saved
    public string ChallengeId { get; set; }
    public string ChallengeSpecId { get; set; }
    public string GameId { get; set; }
    public bool Submit { get; set; }
    public QuestionSubmission[] Questions { get; set; }
}

public class FeedbackQuestion
{
    public string Id { get; set; }
    public string Prompt { get; set; }
    public string ShortName { get; set; }
}

public class QuestionSubmission : FeedbackQuestion
{
    public string Answer { get; set; }
}

public class QuestionTemplate : FeedbackQuestion
{
    public string Type { get; set; } = "text"; // if unspecified in config
    public bool Required { get; set; } = false; // if unspecified in config

    // For 'likert' type questions only
    public int Min { get; set; } = 1;
    public int Max { get; set; }
    public string MinLabel { get; set; }
    public string MaxLabel { get; set; }

    // For 'selectOne' and 'selectMany' type questions only
    public string[] Options { get; set; }
    // Display type
    public string Display { get; set; }
    // Specification for a certain answer
    public QuestionSpecify Specify { get; set; }
}

public class GameFeedbackTemplate
{
    public QuestionTemplate[] Game { get; set; } = [];
    public QuestionTemplate[] Challenge { get; set; } = [];
    public string Message { get; set; }
}


public class Feedback
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string PlayerId { get; set; }
    public string ChallengeId { get; set; }
    public string ChallengeSpecId { get; set; }
    public string GameId { get; set; }
    public QuestionSubmission[] Questions { get; set; }
    public bool Submitted { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class FeedbackReportDetails : Feedback
{
    public string ApprovedName { get; set; }
    public string ChallengeTag { get; set; }
}

public class FeedbackSearchParams : SearchFilter
{
    public const string GameType = "game";
    public const string ChallengeType = "challenge";
    public const string SortOldest = "oldest";
    public const string SortNewest = "newest";
    public const string Submitted = "submitted";
    public string GameId { get; set; }
    public string ChallengeSpecId { get; set; }
    public string ChallengeId { get; set; }
    public string Type { get; set; }
    public string SubmitStatus { get; set; }
    public bool WantsGame => Type == GameType;
    public bool WantsChallenge => Type == ChallengeType;
    public bool WantsSpecificChallenge => Type == ChallengeType && ChallengeSpecId != null;
    public bool WantsSortByTimeNewest => Sort == SortNewest;
    public bool WantsSortByTimeOldest => Sort == SortOldest;
    public bool WantsSubmittedOnly => SubmitStatus == Submitted;
}

// Order of properties below determines order of columns in CSV export
public class FeedbackReportExport
{
    public string UserId { get; set; }
    public string PlayerId { get; set; }
    public string ApprovedName { get; set; }
    public string ChallengeId { get; set; }
    public string ChallengeTag { get; set; }
    public bool Submitted { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public class FeedbackReportHelper : FeedbackReportExport
{
    public Dictionary<string, string> IdToAnswer { get; set; } = new Dictionary<string, string>();
}

public class FeedbackStats
{
    public string ChallengeSpecId { get; set; }
    public int ConfiguredCount { get; set; }
    public int LikertCount { get; set; }
    public int TextCount { get; set; }
    public int SelectOneCount { get; set; }
    public int SelectManyCount { get; set; }
    public int RequiredCount { get; set; }
    public int ResponsesCount { get; set; }
    public int MaxResponseCount { get; set; }
    public int InProgressCount { get; set; }
    public int SubmittedCount { get; set; }
    public IEnumerable<QuestionStats> QuestionStats { get; set; }
}

// Order of properties below determines order of columns in CSV export
public class QuestionStats
{
    public string Id { get; set; }
    public string Prompt { get; set; }
    public string ShortName { get; set; }
    public bool Required { get; set; }
    public double Average { get; set; } // mean of all ratings for this question
    public int ScaleMin { get; set; } // lower bound of likert scale (default 1)
    public int ScaleMax { get; set; } // upper bound of likert scale
    public int Count { get; set; } // how many responses for this question
    public int Lowest { get; set; } // lowest rating given
    public int Highest { get; set; } // highest rating given
}

public class QuestionSpecify
{
    public string Key { get; set; }
    public string Prompt { get; set; }
}

public sealed class DuplicateFeedbackTemplateNameException : GameboardValidationException
{
    public DuplicateFeedbackTemplateNameException(string name)
        : base($"""A feedback template with the name "{name}" already exists. """) { }
}

public sealed class FeedbackTemplateView
{
    public required string Id { get; set; }
    public required string Content { get; set; }
    public required SimpleEntity CreatedBy { get; set; }
    public required string HelpText { get; set; }
    public required string Name { get; set; }
    public required int ResponseCount { get; set; }
    public required IEnumerable<SimpleEntity> UseForGames { get; set; }
    public required IEnumerable<SimpleEntity> UseForGameChallenges { get; set; }
}
