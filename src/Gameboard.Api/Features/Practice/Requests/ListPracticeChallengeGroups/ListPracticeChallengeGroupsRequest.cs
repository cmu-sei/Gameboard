namespace Gameboard.Api.Features.Practice;

public sealed class ListPracticeChallengeGroupsRequest
{
    public bool GetRootOnly { get; set; }
    public string ParentGroupId { get; set; }
    public string SearchTerm { get; set; }
}
