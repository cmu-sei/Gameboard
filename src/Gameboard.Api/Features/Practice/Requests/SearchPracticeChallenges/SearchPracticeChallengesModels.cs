using System.Collections.Generic;

namespace Gameboard.Api.Features.Practice;

public sealed class SearchPracticeChallengesResult
{
    public required PagedEnumerable<PracticeChallengeView> Results { get; set; }
}

public sealed class PracticeChallengeView
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Text { get; set; }
    public required PracticeChallengeViewGame Game { get; set; }
    public required int AverageDeploySeconds { get; set; }
    public required bool IsHidden { get; set; }
    public required string SolutionGuideUrl { get; set; }
    public required IEnumerable<string> Tags { get; set; }
}

public sealed class PracticeChallengeViewGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Logo { get; set; }
    public required bool IsHidden { get; set; }
}
