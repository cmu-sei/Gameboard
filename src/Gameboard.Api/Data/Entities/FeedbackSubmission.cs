// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using Gameboard.Api.Features.Feedback;

namespace Gameboard.Api.Data;

public enum FeedbackSubmissionAttachedEntityType
{
    ChallengeSpec = 0,
    Game = 1
}

public abstract class FeedbackSubmission : IEntity
{
    public string Id { get; set; }
    public FeedbackSubmissionAttachedEntityType AttachedEntityType { get; set; }
    public DateTimeOffset? WhenEdited { get; set; }
    public DateTimeOffset? WhenFinalized { get; set; }
    public required DateTimeOffset WhenCreated { get; set; }

    // json-mapped data (see GameboardDbContext for JSON column config)
    public required ICollection<QuestionSubmission> Responses { get; set; }

    // navs
    public required string FeedbackTemplateId { get; set; }
    public FeedbackTemplate FeedbackTemplate { get; set; }
    public required string UserId { get; set; }
    public Data.User User { get; set; }
}

public class FeedbackSubmissionChallengeSpec : FeedbackSubmission, IEntity
{
    public required string ChallengeSpecId { get; set; }
    public Data.ChallengeSpec ChallengeSpec { get; set; }
}

public class FeedbackSubmissionGame : FeedbackSubmission, IEntity
{
    public required string GameId { get; set; }
    public Data.Game Game { get; set; }
}
