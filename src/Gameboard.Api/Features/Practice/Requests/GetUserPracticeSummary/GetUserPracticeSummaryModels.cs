namespace Gameboard.Api.Features.Practice.Requests;

public sealed class GetUserPracticeSummaryResponse
{
    public required int CountAttempted { get; set; }
    public required int CountAvailable { get; set; }
    public required int CountCompleted { get; set; }
    public required double PointsAvailable { get; set; }
    public required double PointsScored { get; set; }
    public required UserPracticeSummaryResponseTagEngagement[] Tags { get; set; }
}

public sealed class UserPracticeSummaryResponseTagEngagement
{
    public required string Tag { get; set; }
    public required int CountAvailable { get; set; }
    public required int CountAttempted { get; set; }
    public required int CountCompleted { get; set; }
    public required double PointsAvailable { get; set; }
    public required double PointsScored { get; set; }
}
