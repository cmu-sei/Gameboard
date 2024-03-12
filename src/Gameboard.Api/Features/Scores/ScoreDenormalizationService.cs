using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public interface IScoreDenormalizationService
{
    Task DenormalizeGame(string gameId, CancellationToken cancellationToken);
    Task DenormalizeTeam(string teamId, CancellationToken cancellationToken);
}

internal class ScoreDenormalizationService : IScoreDenormalizationService
{
    private readonly INowService _nowService;
    private readonly IScoringService _scoringService;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public ScoreDenormalizationService
    (
        INowService nowService,
        IScoringService scoringService,
        IStore store,
        ITeamService teamService
    )
    {
        _nowService = nowService;
        _scoringService = scoringService;
        _store = store;
        _teamService = teamService;
    }

    public async Task DenormalizeGame(string gameId, CancellationToken cancellationToken)
    {
        var gameScore = await _scoringService.GetGameScore(gameId, cancellationToken);

        // toss old data for this game
        await _store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(s => s.GameId == gameId)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var team in gameScore.Teams)
            await DenormalizeTeam(team, cancellationToken);

        // game now needs reranking because scores may have changed
        await RerankGame(gameId);
    }

    public async Task DenormalizeTeam(string teamId, CancellationToken cancellationToken)
    {
        var teamScore = await _scoringService.GetTeamScore(teamId, cancellationToken);
        await DenormalizeTeam(teamScore, cancellationToken);
    }

    private async Task DenormalizeTeam(TeamScore team, CancellationToken cancellationToken)
    {
        // load game/captain details (captain has the team name)
        var gameId = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.TeamId == team.Team.Id)
            .Select(p => p.GameId)
            .Distinct()
            .SingleAsync(cancellationToken);

        var captain = await _teamService.ResolveCaptain(team.Team.Id, cancellationToken);

        // delete the record associated with this team (if it exists)
        await _store
            .WithNoTracking<DenormalizedTeamScore>()
            .Where(s => s.TeamId == team.Team.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var timeRemaining = captain.SessionEnd - _nowService.Get();

        // add a new denormalized record for the team with a default rank
        await _store.Create(new DenormalizedTeamScore
        {
            GameId = gameId,
            TeamId = team.Team.Id,
            TeamName = captain.ApprovedName,
            Rank = 0,
            ScoreOverall = team.OverallScore.TotalScore,
            ScoreAdvanced = team.OverallScore.AdvancedScore ?? 0,
            ScoreAutoBonus = team.OverallScore.BonusScore,
            ScoreManualBonus = team.OverallScore.ManualBonusScore,
            ScoreChallenge = team.OverallScore.CompletionScore,
            SolveCountNone = team.Challenges.Where(c => c.Result == ChallengeResult.None).Count(),
            SolveCountComplete = team.Challenges.Where(c => c.Result == ChallengeResult.Success).Count(),
            SolveCountPartial = team.Challenges.Where(c => c.Result == ChallengeResult.Partial).Count(),
            CumulativeTimeMs = captain.Time
        });

        // force reranking
        await RerankGame(gameId);
    }

    private async Task RerankGame(string gameId)
    {
        var teams = await _store
            .WithTracking<DenormalizedTeamScore>()
            .Where(t => t.GameId == gameId)
            .ToArrayAsync();

        var rankedTeams = _scoringService.GetTeamRanks(teams.Select(t => new TeamForRanking
        {
            CumulativeTimeMs = t.CumulativeTimeMs,
            OverallScore = t.ScoreOverall,
            TeamId = t.TeamId
        }));

        foreach (var team in teams)
            team.Rank = rankedTeams.ContainsKey(team.TeamId) ? rankedTeams[team.TeamId] : 0;

        await _store.SaveUpdateRange(teams);
    }
}
