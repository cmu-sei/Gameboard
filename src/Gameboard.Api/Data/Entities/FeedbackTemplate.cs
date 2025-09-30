// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;

namespace Gameboard.Api.Data;

public sealed class FeedbackTemplate : IEntity
{
    public string Id { get; set; }
    public string HelpText { get; set; }
    public required string Name { get; set; }
    public required string Content { get; set; }

    public required string CreatedByUserId { get; set; }
    public Data.User CreatedByUser { get; set; }
    public ICollection<FeedbackSubmission> Submissions { get; set; } = [];
    public ICollection<Data.Game> UseAsFeedbackTemplateForGames { get; set; } = [];
    public ICollection<Data.Game> UseAsFeedbackTemplateForGameChallenges { get; set; } = [];
}
