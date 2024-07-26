using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.Scores;

public sealed class Score
{
    public required double? AdvancedScore { get; set; }
    public required double CompletionScore { get; set; }
    public required double ManualBonusScore { get; set; }
    public required double BonusScore { get; set; }
    public required double TotalScore { get; set; }

    public static Score Default
    {
        get => new() { AdvancedScore = 0, CompletionScore = 0, ManualBonusScore = 0, BonusScore = 0, TotalScore = 0 };
    }
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
    public required string SupportKey { get; set; }
}

[JsonDerivedType(typeof(GameScoringChallengeBonusSolveRank), typeDiscriminator: "solveRank")]
public class GameScoringConfigChallengeBonus
{
    public required string Id { get; set; }
    public required string Description { get; set; }
    public required double PointValue { get; set; }
}

public sealed class GameScoringChallengeBonusSolveRank : GameScoringConfigChallengeBonus
{
    public required int SolveRank { get; set; }
}

public class GameScore
{
    public required GameScoreGameInfo Game { get; set; }
    public required IEnumerable<TeamScore> Teams { get; set; }
}

public sealed class GameScoreGameInfo
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required bool IsTeamGame { get; set; }
    public required IEnumerable<GameScoringConfigChallengeSpec> Specs { get; set; }
}

public sealed class TeamScore
{
    public required SimpleEntity Team { get; set; }
    public required IEnumerable<PlayerWithSponsor> Players { get; set; }
    public required bool IsAdvancedToNextRound { get; set; }
    public required Score OverallScore { get; set; }
    public required double CumulativeTimeMs { get; set; }
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

public sealed class TeamForRanking
{
    public required string TeamId { get; set; }
    public required double OverallScore { get; set; }
    public required double CumulativeTimeMs { get; set; }
    public required DateTimeOffset? SessionStart { get; set; }
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
    public required DateTimeOffset? IsLiveUntil { get; set; }
    public required bool IsTeamGame { get; set; }
    public required int SpecCount { get; set; }
}

public sealed class ScoreboardDataTeam
{
    public required string Id { get; set; }
    public required bool IsAdvancedToNextRound { get; set; }
    public required DateTimeOffset? SessionEnds { get; set; }
    public required IEnumerable<PlayerWithSponsor> Players { get; set; }
    public required DenormalizedTeamScore Score { get; set; }
    public required bool UserCanAccessScoreDetail { get; set; }
    public required bool UserIsOnTeam { get; set; }
}
