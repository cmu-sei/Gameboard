using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
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
    public string Id { get; set; }
    public required UpsertFeedbackSubmissionRequestAttachedEntity AttachedEntity { get; set; }
    public required string FeedbackTemplateId { get; set; }
    public IEnumerable<QuestionSubmission> Responses { get; set; }
}

public sealed class UpsertFeedbackSubmissionRequestAttachedEntity
{
    public required string Id { get; set; }
    // [TypeConverter(typeof(JsonStringEnumConverter<FeedbackSubmissionAttachedEntityType>))]
    public required FeedbackSubmissionAttachedEntityType EntityType { get; set; }
    public required string TeamId { get; set; }
}

public sealed class UpsertFeedbackSubmissionResponse
{
    public required FeedbackSubmission Submission { get; set; }
}
