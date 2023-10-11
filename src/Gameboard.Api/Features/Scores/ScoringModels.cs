using System;
using System.Collections.Generic;

public sealed class Score
{
    public double CompletionScore { get; set; }
    public double ManualBonusScore { get; set; }
    public double BonusScore { get; set; }
    public double TotalScore { get; set; }
}

public class GameScoringConfig
{
    public required SimpleEntity Game { get; set; }
    public required IEnumerable<GameScoringConfigChallengeSpec> ChallengeSpecScoringConfigs { get; set; }
}

public sealed class GameScoringConfigChallengeSpec
{
    public required SimpleEntity ChallengeSpec { get; set; }
    public required double CompletionScore { get; set; }
    public required IEnumerable<GameScoringConfigChallengeBonus> PossibleBonuses { get; set; }
    public required double MaxPossibleScore { get; set; }
}

public class GameScoringConfigChallengeBonus
{
    public required string Id { get; set; }
    public required string Description { get; set; }
    public required double PointValue { get; set; }
}

public class GameScore
{
    public required SimpleEntity Game { get; set; }
    public required IEnumerable<GameScoreTeam> Teams { get; set; }
}

public sealed class GameScoreTeam
{
    public SimpleEntity Team { get; set; }
    public IEnumerable<PlayerWithAvatar> Players { get; set; }
    public int Rank { get; set; }
    public IEnumerable<TeamChallengeScoreSummary> Challenges { get; set; }
}

public sealed class GameScoreAutoChallengeBonus
{
    public string Id { get; set; }
    public string Description { get; set; }
    public double PointValue { get; set; }
}

public sealed class TeamChallengeScore
{
    public required SimpleEntity Challenge { get; set; }
    public required SimpleEntity Team { get; set; }
    public required TimeSpan? TimeElapsed { get; set; }
    public required Score Score { get; set; }
}

public class TeamChallengeScoreSummary
{
    public SimpleEntity Challenge { get; set; }
    public required SimpleEntity Spec { get; set; }
    public required SimpleEntity Team { get; set; }
    public required Score Score { get; set; }
    public required TimeSpan? TimeElapsed { get; set; }

    public required IEnumerable<GameScoreAutoChallengeBonus> Bonuses { get; set; }
    public required IEnumerable<ManualChallengeBonusViewModel> ManualBonuses { get; set; }
    public required IEnumerable<GameScoreAutoChallengeBonus> UnclaimedBonuses { get; set; }
}

public class TeamGameScoreSummary
{
    public SimpleEntity Game { get; set; }
    public SimpleEntity Team { get; set; }
    public Score Score { get; set; }
    public IEnumerable<TeamChallengeScoreSummary> ChallengeScoreSummaries { get; set; }
}

public class ChallengeScoreSummary
{
    public SimpleEntity Challenge { get; set; }
    public IEnumerable<TeamChallengeScoreSummary> TeamScores { get; set; }
}
