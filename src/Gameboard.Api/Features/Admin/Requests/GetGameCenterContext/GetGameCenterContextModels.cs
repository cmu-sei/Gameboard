namespace Gameboard.Api.Features.Admin;

public sealed class GameCenterContext
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Logo { get; set; }
    public required DateRange ExecutionWindow { get; set; }
    public required double PointsAvailable { get; set; }
    public required GameCenterContextStats Stats { get; set; }

    public required string Competition { get; set; }
    public required string Season { get; set; }
    public required string Track { get; set; }

    public required int ChallengeCount { get; set; }
    public required int OpenTicketCount { get; set; }
    public required int TotalTicketCount { get; set; }

    public required bool HasScoreboard { get; set; }
    public required bool IsExternal { get; set; }
    public required bool IsLive { get; set; }
    public required bool IsPractice { get; set; }
    public required bool IsPublished { get; set; }
    public required bool IsRegistrationActive { get; set; }
    public required bool IsTeamGame { get; set; }
}

public sealed class GameCenterContextStats
{
    public required int? AttemptCountPractice { get; set; }
    public int PlayerCountActive { get; set; }
    public int PlayerCountCompetitive { get; set; }
    public int PlayerCountPractice { get; set; }
    public int PlayerCountTotal { get; set; }
    public int TeamCountActive { get; set; }
    public int TeamCountCompetitive { get; set; }
    public int TeamCountComplete { get; set; }
    public int TeamCountPractice { get; set; }
    public int TeamCountNotStarted { get; set; }
    public int TeamCountTotal { get; set; }
    public required double? TopScore { get; set; }
    public string TopScoreTeamName { get; set; }
}
