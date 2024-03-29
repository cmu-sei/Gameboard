using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.UnityGames;

public interface IUnityStore : IStore<Data.ChallengeSpec>
{
    Task<Data.ChallengeEvent> AddUnityChallengeEvent(Data.ChallengeEvent challengeEvent);
    Task<Data.Challenge> HasChallengeData(string gamespaceId);
}

internal class UnityStore : Store<Data.ChallengeSpec>, IUnityStore
{
    public UnityStore(GameboardDbContext dbContext, IGuidService guids)
        : base(dbContext, guids) { }

    public async Task<Data.ChallengeEvent> AddUnityChallengeEvent(Data.ChallengeEvent challengeEvent)
    {
        this.DbContext.ChallengeEvents.Add(challengeEvent);
        await this.DbContext.SaveChangesAsync();

        return challengeEvent;
    }

    public async Task<Data.Challenge> HasChallengeData(string gamespaceId)
    {
        return await DbContext
            .Challenges
            .AsNoTracking()
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.Id == gamespaceId);
    }
}
