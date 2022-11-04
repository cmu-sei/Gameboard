using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.UnityGames;

public class UnityStore : Store<Data.ChallengeSpec>, IUnityStore
{
    public UnityStore(GameboardDbContext dbContext)
        : base(dbContext) { }


    public async Task<IEnumerable<Data.ChallengeEvent>> AddUnityChallengeEvents(IEnumerable<Data.ChallengeEvent> challengeEvents)
    {
        this.DbContext.ChallengeEvents.AddRange(challengeEvents);
        await this.DbContext.SaveChangesAsync();

        return challengeEvents;
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