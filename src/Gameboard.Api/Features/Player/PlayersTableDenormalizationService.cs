using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Players;

/// <summary>
/// Okay. Stick with me.
/// 
/// The Players table contains denormalized data that can be derived from
/// the Challenges table (like score, which is the sum of the challenge scores 
/// plus bonuses, and time, which is the total of the sum of the differences between 
/// challenge start/end time). 
/// 
/// We denormalize the data because we show it on the scoreboard, and the scoreboard
/// automatically refreshes itself periodically (10 seconds-ish?) so we don't want to manually
/// query all this data every time. To make this work we manually fire this logic off when one 
/// of the pieces of data that we denormalize here changes.
/// </summary>
public interface IPlayersTableDenormalizationService
{
    Task UpdateTeamData(string teamId, CancellationToken cancellationToken);
}

internal class PlayersTableDenormalizationService : IPlayersTableDenormalizationService
{
    private readonly IStore _store;

    public PlayersTableDenormalizationService(IStore store)
    {
        _store = store;
    }

    public async Task UpdateTeamData(string teamId, CancellationToken cancellationToken)
    {
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.TeamId == teamId)
            .ToArrayAsync(cancellationToken);

        var score = (int)challenges.Sum(c => c.Score);
        var time = challenges.Sum(c => c.Duration);
        var complete = challenges.Count(c => c.Result == ChallengeResult.Success);
        var partial = challenges.Count(c => c.Result == ChallengeResult.Partial);

        await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .ExecuteUpdateAsync
            (
                up => up
                    .SetProperty(p => p.CorrectCount, complete)
                    .SetProperty(p => p.PartialCount, partial)
                    .SetProperty(p => p.Score, score)
                    .SetProperty(p => p.Time, time)
            );
    }

    /* 
        public async Task UpdateRanks(string gameId)
        {
            var players = await DbContext.Players
                .Where(p => p.GameId == gameId)
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Time)
                .ThenByDescending(p => p.CorrectCount)
                .ThenByDescending(p => p.PartialCount)
                .ToArrayAsync()
            ;
            int rank = 0;

            foreach (var team in players.GroupBy(p => p.TeamId))
            {
                rank += 1;
                foreach (var player in team)
                    player.Rank = rank;
            }

            await DbContext.SaveChangesAsync();
        }
    */
}
