using System;

namespace Gameboard.Api.Features.Practice;

public sealed class GetPracticeChallengeGroupResponse
{
    public required GetPracticeChallengeGroupResponseGroup Group { get; set; }
    public required SimpleEntity ParentGroup { get; set; }
    public required GetPracticeChallengeGroupResponseGroup[] ChildGroups { get; set; }
}

public sealed class GetPracticeChallengeGroupResponseGroup
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string ImageUrl { get; set; }
    public required bool IsFeatured { get; set; }
    public required GetPracticeChallengeGroupResponseChallenge[] Challenges { get; set; }
}

public sealed class GetPracticeChallengeGroupResponseChallenge
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required int CountCompleted { get; set; }
    public required int CountLaunched { get; set; }
    public required DateTimeOffset? LastLaunched { get; set; }
}
