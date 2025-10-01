// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Gameboard.Api.Features.Feedback;

public sealed class ListFeedbackTemplatesResponse
{
    public required IEnumerable<FeedbackTemplateView> Templates { get; set; }
}
