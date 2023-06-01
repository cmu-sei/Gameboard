using System;
using System.Collections.Generic;
using Gameboard.Api.Common;

public sealed class ConfigureGameAutoBonusesCommandParameters
{
    public required string GameId { get; set; }
    public required GameAutomaticBonusesConfig Config { get; set; }
}

public class GameAutomaticBonusesConfig
{
    public IEnumerable<GameAutomaticBonusSolveRank> AllChallengesBonuses { get; set; }
    public IEnumerable<PerChallengeAutomaticBonusSolveRank> SpecificChallengesBonuses { get; set; }
}

public sealed class GameAutomaticBonusSolveRank
{
    public string Description { get; set; }
    public required double PointValue { get; set; }
    public required int SolveRank { get; set; }
}

public sealed class GameSpecsAutomaticBonusState
{
    public required string GameId { get; set; }
    public required IDictionary<string, GameSpecsAutomaticBonusSpecState> SpecStates { get; set; }
}

public sealed class GameSpecsAutomaticBonusSpecState
{
    public required IEnumerable<string> AwardedBonusesIds { get; set; }
    public required IEnumerable<string> UnawardedBonusIds { get; set; }
}

public sealed class PerChallengeAutomaticBonusSolveRank
{
    public required string SupportKey { get; set; }
    public string Description { get; set; }
    public required double PointValue { get; set; }
    public required int SolveRank { get; set; }
}

public class CreateManualChallengeBonus
{
    public string Description { get; set; }
    public double PointValue { get; set; }
}

public class UpdateManualChallengeBonus
{
    public string Id { get; set; }
    public string Description { get; set; }
    public double PointValue { get; set; }
}

public class ManualChallengeBonusViewModel
{
    public string Id { get; set; }
    public string Description { get; set; }
    public double PointValue { get; set; }
    public DateTimeOffset EnteredOn { get; set; }
    public SimpleEntity EnteredBy { get; set; }
    public string ChallengeId { get; set; }
}
