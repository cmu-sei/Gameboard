using System.Collections.Generic;
using Gameboard.Api.Structure;

public class TeamChallengeScoreSummary
{
    public SimpleEntity Team { get; set; }
    public double TotalScore { get; set; }
    public double BaseScore { get; set; }
    public double BonusScore { get; set; }
    public IEnumerable<ManualChallengeBonusViewModel> ManualBonuses { get; set; }
}

public class ChallengeScoreSummary
{
    public SimpleEntity Challenge { get; set; }
    public IEnumerable<TeamChallengeScoreSummary> TeamScores { get; set; }
}
