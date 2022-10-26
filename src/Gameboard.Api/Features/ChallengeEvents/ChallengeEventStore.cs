using Gameboard.Api.Data;

namespace Gameboard.Api.Features.ChallengeEvents;

public class ChallengeEventStore : Store<ChallengeEvent>, IChallengeEventStore
{
    public ChallengeEventStore(GameboardDbContext dbContext)
    : base(dbContext) { }
}