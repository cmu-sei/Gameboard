namespace Gameboard.Api.Features.Practice;

public record GetUserChallengeGroupsRequest(string UserId, string GroupId, string ParentGroupId, string SearchTerm);
