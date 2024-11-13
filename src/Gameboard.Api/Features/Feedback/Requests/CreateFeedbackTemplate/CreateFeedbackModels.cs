namespace Gameboard.Api.Features.Feedback;

public sealed class CreateFeedbackTemplateRequest
{
    public required string Content { get; set; }
    public string HelpText { get; set;}
    public required string Name { get; set; }
}
