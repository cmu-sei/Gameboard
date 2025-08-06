using System;

namespace Gameboard.Api.Features.Practice;

public sealed class GetUserChallengeGroupsResponse
{
    public GetUserChallengeGroupsResponseGroup[] Groups { get; set; }
}

public sealed class GetUserChallengeGroupsResponseGroup
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string ImageUrl { get; set; }
    public required bool IsFeatured { get; set; }
    public required int ChallengeCount { get; set; }
    public required int ChallengePoints { get; set; }
    public required GetUserChallengeGroupsResponseChallenge[] Challenges { get; set; }
    public required string[] Tags { get; set; }
    public required SimpleEntity ParentGroup { get; set; }
    public required SimpleEntity[] ChildGroups { get; set; }
}

public sealed class GetUserChallengeGroupsResponseChallenge
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required bool HasCertificateTemplate { get; set; }
    public required string[] Tags { get; set; }
    public required double MaxPossibleScore { get; set; }
    public required GetUserChallengeGroupsResponseChallengeAttempt BestAttempt { get; set; }
}

public sealed class GetUserChallengeGroupsResponseChallengeAttempt
{
    public required bool CertificateAwarded { get; set; }
    public required DateTimeOffset Date { get; set; }
    public required double Score { get; set; }
}
