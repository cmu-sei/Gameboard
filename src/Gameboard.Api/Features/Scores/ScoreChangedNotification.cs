using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public class ScoreChangedNotification : INotification
{
    private readonly string _teamId;
    public string TeamId { get => _teamId; }

    public ScoreChangedNotification(string teamId)
    {
        _teamId = teamId;
    }
}

internal class ScoreChangedNotificationHandler : INotificationHandler<ScoreChangedNotification>
{
    private readonly IScoreDenormalizationService _scoreDenormalizationService;
    private readonly IStore _store;

    public ScoreChangedNotificationHandler
    (
        IScoreDenormalizationService scoringDenormalizationService,
        IStore store
    )
    {
        _scoreDenormalizationService = scoringDenormalizationService;
        _store = store;
    }

    public async Task Handle(ScoreChangedNotification notification, CancellationToken cancellationToken)
    {
        // first to the legacy logic (which also includes updating the players table)
        // we can drop this when we're confident in the new scoreboard and reassess how we'll
        // handle the Score/Rank/Time etc. in the Players table
        await DoLegacyRerank(notification.TeamId, cancellationToken);
        await _scoreDenormalizationService.DenormalizeTeam(notification.TeamId, cancellationToken);
    }

    private async Task DoLegacyRerank(string teamId, CancellationToken cancellationToken)
    {
        // update the team's scores (this fires after changes to the score)
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
                .Include(c => c.AwardedBonuses)
                    .ThenInclude(b => b.ChallengeBonus)
                .Include(c => c.AwardedManualBonuses)
            .Where(c => c.TeamId == teamId)
            .Where(c => c.PlayerMode == PlayerMode.Competition)
            .ToArrayAsync(cancellationToken);

        var score = (int)challenges.Sum(c => c.Score);
        var time = challenges.Sum(c => c.Duration);
        var complete = challenges.Count(c => c.Result == ChallengeResult.Success);
        var partial = challenges.Count(c => c.Result == ChallengeResult.Partial);

        // we do this with tracking for the convenience of knowing which gameId we're affecting
        await _store.DoTransaction(async ctx =>
        {
            // determine which game's ranks we're manipulating
            var game = await ctx
                .Games
                .Include(g => g.Players)
                .Where(g => g.Players.Any(p => p.TeamId == teamId))
                .SingleAsync(cancellationToken);

            // find the players on the scoring team and update their scores
            var teamPlayers = game.Players.Where(p => p.TeamId == teamId);
            if (!teamPlayers.Any())
                return;

            foreach (var player in teamPlayers)
            {
                player.CorrectCount = complete;
                player.PartialCount = partial;
                player.Score = score;
                player.Time = time;
            }

            // then update the ranks of every player in the game based on the changes
            var gamePlayers = game.Players
                .OrderByDescending(p => p.Score)
                    .ThenBy(p => p.Time)
                    .ThenByDescending(p => p.CorrectCount)
                    .ThenByDescending(p => p.PartialCount)
                    .ToArray()
                    .GroupBy(p => p.TeamId);

            var teamRank = 0;
            foreach (var team in gamePlayers)
            {
                teamRank += 1;
                foreach (var player in team)
                    player.Rank = teamRank;
            }

            // save it up
            await ctx.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }
}
