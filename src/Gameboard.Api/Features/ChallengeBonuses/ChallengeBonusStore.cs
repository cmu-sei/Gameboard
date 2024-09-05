using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.ChallengeBonuses;

public interface IChallengeBonusStore : IStore<Data.ChallengeBonus> { }

internal class ChallengeBonusStore(IDbContextFactory<GameboardDbContext> dbContextFactory, IGuidService guids)
    : Store<Data.ChallengeBonus>(dbContextFactory, guids), IChallengeBonusStore
{
}
