using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Features.ChallengeBonuses;

public interface IChallengeBonusStore : IStore<Data.ChallengeBonus> { }

internal class ChallengeBonusStore : Store<Data.ChallengeBonus>, IChallengeBonusStore
{
    public ChallengeBonusStore(GameboardDbContext dbContext, IGuidService guids)
        : base(dbContext, guids) { }
}
