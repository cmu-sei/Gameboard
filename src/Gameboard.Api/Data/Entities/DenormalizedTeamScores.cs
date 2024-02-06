namespace Gameboard.Api.Data;

public class DenormalizedTeamScore : IEntity
{
    public string Id { get; set; }
    public required string TeamId { get; set; }
    public required string TeamName { get; set; }

    public required double ScoreOverall { get; set; }
    public required double ScoreAutoBonus { get; set; }
    public required double ScoreManualBonus { get; set; }
    public required double ScoreChallenge { get; set; }

    public required int SolveCountNone { get; set; }
    public required int SolveCountPartial { get; set; }
    public required int SolveCountComplete { get; set; }

    public required double CumulativeTimeMs { get; set; }
    public required double? TimeRemainingMs { get; set; }

    public required string GameId { get; set; }
    public Data.Game Game { get; set; }
}
