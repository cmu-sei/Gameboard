// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

public sealed class ConfigureGameAutoBonusesCommandParameters
{
    public required string GameId { get; set; }
    public required GameAutomaticBonusesConfig Config { get; set; }
}

public class GameAutomaticBonusesConfig
{
    public IEnumerable<GameAutomaticBonusSolveRank> AllChallengesBonuses { get; set; } = new List<GameAutomaticBonusSolveRank>();
    public IEnumerable<PerChallengeAutomaticBonusSolveRank> SpecificChallengesBonuses { get; set; } = new List<PerChallengeAutomaticBonusSolveRank>();
}

public sealed class GameAutomaticBonusSolveRank
{
    public string Description { get; set; }
    public required double PointValue { get; set; }
    public required int SolveRank { get; set; }
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

public class CreateManualBonus
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

public abstract class ManualBonusViewModel
{
    public string Id { get; set; }
    public string Description { get; set; }
    public double PointValue { get; set; }
    public DateTimeOffset EnteredOn { get; set; }
    public SimpleEntity EnteredBy { get; set; }
}

public class ManualChallengeBonusViewModel : ManualBonusViewModel
{
    public string ChallengeId { get; set; }
}

public class ManualTeamBonusViewModel : ManualBonusViewModel
{
    public string TeamId { get; set; }
}
