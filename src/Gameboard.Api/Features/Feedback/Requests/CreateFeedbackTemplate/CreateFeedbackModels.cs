// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Features.Feedback;

public sealed class CreateFeedbackTemplateRequest
{
    public required string Content { get; set; }
    public string HelpText { get; set;}
    public required string Name { get; set; }
}
