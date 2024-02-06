using System;
using System.Collections.Generic;
using Gameboard.Api;
using Gameboard.Api.Data;

public sealed class Score
{
    public double CompletionScore { get; set; }
    public double ManualBonusScore { get; set; }
    public double BonusScore { get; set; }
    public double TotalScore { get; set; }
}

public sealed class GameScoringConfig
{
    public required SimpleEntity Game { get; set; }
    public required IEnumerable<GameScoringConfigChallengeSpec> Specs { get; set; }
}

public sealed class GameScoringConfigChallengeSpec
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
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
    public required GameScoreGameInfo Game { get; set; }
    public required IEnumerable<GameScoreTeam> Teams { get; set; }
}

public sealed class GameScoreGameInfo
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsTeamGame { get; set; }
    public required IEnumerable<GameScoringConfigChallengeSpec> Specs { get; set; }
}

public sealed class GameScoreTeam
{
    public required SimpleEntity Team { get; set; }
    public required IEnumerable<PlayerWithSponsor> Players { get; set; }
    public required DateTimeOffset? LiveSessionEnds { get; set; }
    public required Score OverallScore { get; set; }
    public required double TotalTimeMs { get; set; }
    public required double? RemainingTimeMs { get; set; }
    public required IEnumerable<ManualTeamBonusViewModel> ManualBonuses { get; set; } = Array.Empty<ManualTeamBonusViewModel>();
    public required IEnumerable<TeamChallengeScore> Challenges { get; set; }
}

public sealed class GameScoreAutoChallengeBonus
{
    public string Id { get; set; }
    public string Description { get; set; }
    public double PointValue { get; set; }
}

public class TeamChallengeScore
{
    public required string Id { get; set; }
    public required string SpecId { get; set; }
    public required string Name { get; set; }
    public required ChallengeResult Result { get; set; }
    public required Score Score { get; set; }
    public required TimeSpan? TimeElapsed { get; set; }

    public required IEnumerable<GameScoreAutoChallengeBonus> Bonuses { get; set; }
    public required IEnumerable<ManualChallengeBonusViewModel> ManualBonuses { get; set; }
    public required IEnumerable<GameScoreAutoChallengeBonus> UnclaimedBonuses { get; set; }
}

// model explicitly for the denormalized "/scoreboard" endpoint
public sealed class ScoreboardData
{
    public required ScoreboardDataGame Game { get; set; }
    public required IEnumerable<ScoreboardDataTeam> Teams { get; set; } = new List<ScoreboardDataTeam>();
}

public sealed class ScoreboardDataGame
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsTeamGame { get; set; }
    public required int SpecCount { get; set; }
}

public sealed class ScoreboardDataTeam
{
    public IEnumerable<PlayerWithSponsor> Players { get; set; }
    public DenormalizedTeamScore Score { get; set; }
}
