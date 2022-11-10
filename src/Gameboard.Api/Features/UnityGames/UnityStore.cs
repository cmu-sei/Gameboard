using System.Threading.Tasks;
using Gameboard.Api.Data;

namespace Gameboard.Api.Features.UnityGames;

public class UnityStore : Store<Data.ChallengeSpec>, IUnityStore
{
    public UnityStore(GameboardDbContext dbContext)
        : base(dbContext) { }


    public async Task<Data.ChallengeEvent> AddUnityChallengeEvent(Data.ChallengeEvent challengeEvent)
    {
        this.DbContext.ChallengeEvents.Add(challengeEvent);
        await this.DbContext.SaveChangesAsync();

        return challengeEvent;
    }

    // public async Task UpdateAvgDeployTime(string gameId)
    // {
    //     var stats = await DbContext.Challenges
    //         .Where(g => g.Id == gameId)
    //         .Where(c => c.Game. == c.Game.SpecId)
    //         // .Select(c => new { Created = c.WhenCreated, Started = c.StartTime })
    //         // .OrderByDescending(m => m.Created)
    //         // .Take(20)
    //         .ToArrayAsync();

    //         int avg = (int) stats.Average(m =>
    //             m.Started.Subtract(m.Created).TotalSeconds
    //         );

    //         var spec = await DbContext.ChallengeSpecs.FindAsync(specId);

    //         spec.AverageDeploySeconds = avg;

    //         await DbContext.SaveChangesAsync();
    // }

}