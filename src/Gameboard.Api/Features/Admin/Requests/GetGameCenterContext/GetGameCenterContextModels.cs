namespace Gameboard.Api.Features.Admin;

public sealed class GameCenterContext
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Logo { get; set; }
    public required DateRange ExecutionWindow { get; set; }
    public required double PointsAvailable { get; set; }

    public required string Competition { get; set; }
    public required string Season { get; set; }
    public required string Track { get; set; }

    public required int ChallengeCount { get; set; }
    public required int OpenTicketCount { get; set; }

    public required bool IsExternal { get; set; }
    public required bool IsLive { get; set; }
    public required bool IsPractice { get; set; }
    public required bool IsRegistrationActive { get; set; }
    public required bool IsTeamGame { get; set; }
}
