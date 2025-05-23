namespace Gameboard.Api.Features.Feedback;

public sealed class UpdateFeedbackTemplateRequest
{
    public required string Id { get; set; }
    public required string Content { get; set; }
    public string HelpText { get; set; }
    public required string Name { get; set; }
}
