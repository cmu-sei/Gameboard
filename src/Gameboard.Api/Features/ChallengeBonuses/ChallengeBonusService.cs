using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gameboard.Api.Features.ChallengeBonuses;

internal class ChallengeBonusService
{
    private readonly IChallengeBonusStore _challengeBonusStore;

    public ChallengeBonusService(IChallengeBonusStore challengeBonusStore)
    {
        _challengeBonusStore = challengeBonusStore;
    }

    // public Task ConfigureSpecAutomaticBonuses(string specId, IEnumerable<Data.ChallengeBonus> bonuses)
    // {

    //     _challengeBonusStore.Create(new Data.ChallengeBonus
    //     {

    //     });
    // }
}
