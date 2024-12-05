using System.Collections.Generic;

namespace Gameboard.Api.Data;

public sealed class FeedbackTemplate : IEntity
{
    public string Id { get; set; }
    public string HelpText { get; set;}
    public required string Name { get; set; }
    public required string Content { get; set; }

    public required string CreatedByUserId { get; set; }
    public Data.User CreatedByUser { get; set; }
    public required ICollection<Data.Game> UseAsFeedbackTemplateForGames { get; set; } = [];
    public required ICollection<Data.Game> UseAsFeedbackTemplateForGameChallenges { get; set; } = [];
}
