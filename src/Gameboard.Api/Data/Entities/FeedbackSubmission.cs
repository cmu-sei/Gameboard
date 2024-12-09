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
    public required FeedbackSubmissionAttachedEntityType AttachedEntityType { get; set; }
    public DateTimeOffset? WhenEdited { get; set; }
    public required DateTimeOffset WhenSubmitted { get; set; }

    // json-mapped data (see GameboardDbContext for JSON column config)
    public required ICollection<QuestionSubmission> Responses { get; set; }

    // navs
    public required string TeamId { get; set; }
    public required string FeedbackTemplateId { get; set; }
    public required FeedbackTemplate FeedbackTemplate { get; set; }
    public required string UserId { get; set; }
    public required Data.User User { get; set; }
}

public class FeedbackSubmissionChallengeSpec : FeedbackSubmission, IEntity
{
    public required string ChallengeSpecId { get; set; }
    public required Data.ChallengeSpec ChallengeSpec { get; set; }
}

public class FeedbackSubmissionGame : FeedbackSubmission, IEntity
{
    public required string GameId { get; set; }
    public required Data.Game Game { get; set; }
}
