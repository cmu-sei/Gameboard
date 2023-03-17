using System.Collections.Generic;
using Gameboard.Api.Structure;

public class TeamChallengeScoreSummary
{
    public required SimpleEntity Challenge { get; set; }
    public required SimpleEntity Team { get; set; }
    public required double TotalScore { get; set; }
    public required double ScoreFromChallenge { get; set; }
    public required double ScoreFromManualBonuses { get; set; }
    public required IEnumerable<ManualChallengeBonusViewModel> ManualBonuses { get; set; }
}

public class TeamGameScoreSummary
{
    public SimpleEntity Game { get; set; }
    public SimpleEntity Team { get; set; }
    public double TotalScore { get; set; }
    public double ChallengesScore { get; set; }
    public double ManualBonusesScore { get; set; }
    public IEnumerable<TeamChallengeScoreSummary> ChallengeScoreSummaries { get; set; }
}

public class ChallengeScoreSummary
{
    public SimpleEntity Challenge { get; set; }
    public IEnumerable<TeamChallengeScoreSummary> TeamScores { get; set; }
}
