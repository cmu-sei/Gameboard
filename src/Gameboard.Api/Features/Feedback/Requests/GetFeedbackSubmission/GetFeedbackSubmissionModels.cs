// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Feedback;

public sealed class GetFeedbackSubmissionRequest
{
    public required string EntityId { get; set; }
    public required FeedbackSubmissionAttachedEntityType EntityType { get; set; }
    public required string UserId { get; set; }
}
