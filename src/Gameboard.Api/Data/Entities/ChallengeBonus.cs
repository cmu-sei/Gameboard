// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Gameboard.Api.Data;

public enum ChallengeBonusType
{
    CompleteSolveRank = 0
}

public abstract class ChallengeBonus : IEntity
{
    public string Id { get; set; }
    public string Description { get; set; }
    public double PointValue { get; set; }
    public ChallengeBonusType ChallengeBonusType { get; set; }

    public string ChallengeSpecId { get; set; }
    public ChallengeSpec ChallengeSpec { get; set; }

    public ICollection<AwardedChallengeBonus> AwardedTo { get; set; } = new List<AwardedChallengeBonus>();
}

public class ChallengeBonusCompleteSolveRank : ChallengeBonus
{
    public int SolveRank { get; set; }
}

public class AwardedChallengeBonus : IEntity
{
    public string Id { get; set; }
    public DateTimeOffset EnteredOn { get; set; }
    public string InternalSummary { get; set; }

    // nav properties
    public string ChallengeBonusId { get; set; }
    public ChallengeBonus ChallengeBonus { get; set; }

    public string ChallengeId { get; set; }
    public Challenge Challenge { get; set; }
}

public enum ManualBonusType
{
    Challenge = 0,
    Manual = 1
}

public abstract class ManualBonus : IEntity
{
    public string Id { get; set; }
    public string Description { get; set; }
    public DateTimeOffset EnteredOn { get; set; }
    public double PointValue { get; set; }
    public ManualBonusType Type { get; set; }

    // nav properties
    public string EnteredByUserId { get; set; }
    public User EnteredByUser { get; set; }
}

public class ManualChallengeBonus : ManualBonus
{
    // adds an associated challenge
    public string ChallengeId { get; set; }
    public Challenge Challenge { get; set; }
}

public class ManualTeamBonus : ManualBonus
{
    public string TeamId { get; set; }
}
