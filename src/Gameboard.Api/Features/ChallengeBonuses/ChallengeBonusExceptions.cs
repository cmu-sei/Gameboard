// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.ChallengeBonuses;

public sealed class CantResolveManualBonusType : GameboardValidationException
{
    public CantResolveManualBonusType(string manualBonusId)
        : base($"Couldn't resolve the type of manual bonus {manualBonusId}") { }
}

public sealed class GameAutoBonusCantBeNonPositive : GameboardValidationException
{
    public GameAutoBonusCantBeNonPositive(string gameId, IEnumerable<double> pointValues)
        : base($"""Can't configure automatic challenge bonus(es) for game "{gameId}" with non-positive point values ($"{string.Join(",", pointValues)}").""") { }
}

public sealed class CantDeleteAutoBonusIfAwarded : GameboardValidationException
{
    public CantDeleteAutoBonusIfAwarded(string gameId, IEnumerable<string> awardedBonusIds)
        : base($"""Can't delete configured automatic challenge bonuses for game "{gameId}" because bonuses have been awarded for it ({string.Join(",", awardedBonusIds)}).""") { }
}

public sealed class InvalidManualBonusConfiguration : GameboardValidationException
{
    public InvalidManualBonusConfiguration(string challengeId, string teamId)
        : base($"When creating a manual bonus, you must either associate it with a team or challenge (and not both). ChallengeId: {challengeId}. TeamId: {teamId}") { }
}
