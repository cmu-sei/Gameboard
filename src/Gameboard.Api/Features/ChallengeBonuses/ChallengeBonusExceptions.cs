using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.ChallengeBonuses;

public sealed class GameAutoBonusCantBeNonPositive : GameboardValidationException
{
    public GameAutoBonusCantBeNonPositive(string gameId, IEnumerable<double> pointValues)
        : base($"""Can't configure automatic challenge bonus(es) for game "{gameId}" with non-positive point values ($"{string.Join(",", pointValues)}").""") { }
}
