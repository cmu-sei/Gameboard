using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Feedback;

public sealed class GetFeedbackSubmissionRequest
{
    public required string EntityId { get; set; }
    public required FeedbackSubmissionAttachedEntityType EntityType { get; set; }
    public required string UserId { get; set; }
}
