using System.Collections.Generic;

namespace Gameboard.Api.Data;

public sealed class FeedbackTemplate
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Content { get; set; }

    public required string CreatedByUserId { get; set; }
    public required Data.User CreatedByUser { get; set; }
    public required ICollection<Data.Game> UseForGames { get; set; } = [];
    public required ICollection<Data.ChallengeSpec> UseForChallengeSpecs { get; set; } = [];
}
