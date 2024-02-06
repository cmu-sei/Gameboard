using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
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
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public ScoreChangedNotificationHandler
    (
        IScoringService scoringService,
        IStore store,
        ITeamService teamService
    )
    {
        _scoringService = scoringService;
        _store = store;
        _teamService = teamService;
    }

    public async Task Handle(ScoreChangedNotification notification, CancellationToken cancellationToken)
    {
        await DoLegacyRerank(notification.TeamId, cancellationToken);
        await DenormalizeScore(notification.TeamId, cancellationToken);
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

    private async Task DenormalizeScore(string teamId, CancellationToken cancellationToken)
    {
        var updatedScore = await _scoringService.GetTeamScore(teamId);
        var gameId = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == teamId)
            .Select(p => p.GameId)
            .SingleAsync(cancellationToken);

        var captain = await _teamService.ResolveCaptain(teamId, cancellationToken);

        await _store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(s => s.TeamId == teamId)
            .ExecuteDeleteAsync(cancellationToken);

        await _store.Create(new DenormalizedTeamScore
        {
            GameId = gameId,
            TeamId = teamId,
            TeamName = captain.ApprovedName,
            ScoreOverall = updatedScore.OverallScore.TotalScore,
            ScoreAutoBonus = updatedScore.OverallScore.BonusScore,
            ScoreManualBonus = updatedScore.OverallScore.ManualBonusScore,
            ScoreChallenge = updatedScore.OverallScore.CompletionScore,
            SolveCountNone = updatedScore.Challenges.Where(c => c.Result == ChallengeResult.None).Count(),
            SolveCountComplete = updatedScore.Challenges.Where(c => c.Result == ChallengeResult.Success).Count(),
            SolveCountPartial = updatedScore.Challenges.Where(c => c.Result == ChallengeResult.Partial).Count(),
            CumulativeTimeMs = updatedScore.TotalTimeMs,
            TimeRemainingMs = updatedScore.RemainingTimeMs
        });
    }
}
