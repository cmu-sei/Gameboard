using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.ChallengeBonuses;

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
