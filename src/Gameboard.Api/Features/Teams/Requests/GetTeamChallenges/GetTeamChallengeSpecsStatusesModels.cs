namespace Gameboard.Api.Features.Teams;

public sealed class GetTeamChallengeSpecsStatusesResponse
{
    public required SimpleEntity Team { get; set; }
    public required SimpleEntity Game { get; set; }
    public required TeamChallengeSpecStatus[] ChallengeSpecStatuses { get; set; }
}

public enum TeamChallengeSpecStatusState
{
    NotStarted,
    NotDeployed,
    Deployed,
    Ended
}

public sealed class TeamChallengeSpecStatus
{
    public required DateRange AvailabilityRange { get; set; }
    public required string ChallengeId { get; set; }
    public required double? Score { get; set; }
    public required double ScoreMax { get; set; }
    public required SimpleEntity Spec { get; set; }
    public required TeamChallengeSpecStatusState State { get; set; }
}
