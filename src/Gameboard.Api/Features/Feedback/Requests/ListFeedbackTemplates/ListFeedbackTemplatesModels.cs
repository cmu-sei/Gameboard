using System.Collections.Generic;

namespace Gameboard.Api.Features.Feedback;

public sealed class ListFeedbackTemplatesResponse
{
    public required IEnumerable<FeedbackTemplateView> Templates { get; set; }
}
