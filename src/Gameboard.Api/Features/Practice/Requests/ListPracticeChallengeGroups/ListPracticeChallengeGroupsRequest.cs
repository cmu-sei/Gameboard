namespace Gameboard.Api.Features.Practice;

public sealed class ListPracticeChallengeGroupsRequest
{
    public string ContainChallengeSpecId { get; set; }
    public bool GetRootOnly { get; set; }
    public string ParentGroupId { get; set; }
    public string SearchTerm { get; set; }
}
