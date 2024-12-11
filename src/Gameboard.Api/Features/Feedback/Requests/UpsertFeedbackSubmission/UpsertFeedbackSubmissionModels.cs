using System.Collections.Generic;
using Gameboard.Api.Data;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Feedback;

public sealed class InvalidFeedbackTemplateId : GameboardValidationException
{
    public InvalidFeedbackTemplateId(string id, FeedbackSubmissionAttachedEntityType type, string entityId)
        : base($"The template id {id} was not valid for {type} {entityId}") { }
}

public sealed class UpsertFeedbackSubmissionRequest
{
    public required FeedbackSubmissionAttachedEntity AttachedEntity { get; set; }
    public required string FeedbackTemplateId { get; set; }
    public required bool IsFinalized { get; set; }
    public IEnumerable<QuestionSubmission> Responses { get; set; }
}

public sealed class UpsertFeedbackSubmissionResponse
{
    public required FeedbackSubmissionView Submission { get; set; }
}
