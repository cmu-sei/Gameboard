namespace Gameboard.Api.Features.Practice;

public sealed class ListPracticeChallengeGroupsResponse
{
    public ListPracticeChallengeGroupsResponseGroup[] Groups { get; set; }
}

public sealed class ListPracticeChallengeGroupsResponseGroup
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string ImageUrl { get; set; }
    public required bool IsFeatured { get; set; }
    public required int ChallengeCount { get; set; }
    public required string ParentGroupId { get; set; }
    public ListPracticeChallengeGroupsResponseGroup[] ChildGroups { get; set; }
}
