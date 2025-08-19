using System;

namespace Gameboard.Api.Features.Practice;

public sealed class GetPracticeChallengeGroupsUserDataResponse
{
    public GetPracticeChallengeGroupsUserDataResponseGroup[] Groups { get; set; }
}

public sealed class GetPracticeChallengeGroupsUserDataResponseGroup
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required int ChallengeCount { get; set; }
    public required int ChallengeMaxScoreTotal { get; set; }
    public required GetPracticeChallengeGroupsUserDataResponseChallenge[] Challenges { get; set; }
    public required GetPracticeChallengeGroupsUserDataResponseUserData UserData { get; set; }
}

public sealed class GetPracticeChallengeGroupsUserDataResponseChallenge
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool HasCertificateTemplate { get; set; }
    public required double MaxPossibleScore { get; set; }
    public required GetPracticeChallengeGroupsUserDataChallengeAttempt BestAttempt { get; set; }
}

public sealed class GetPracticeChallengeGroupsUserDataChallengeAttempt
{
    public required bool CertificateAwarded { get; set; }
    public required DateTimeOffset Date { get; set; }
    public required double Score { get; set; }
}

public sealed class GetPracticeChallengeGroupsUserDataResponseUserData
{
    public required int ChallengesCompleteCount { get; set; }
    public required double Score { get; set; }
}
