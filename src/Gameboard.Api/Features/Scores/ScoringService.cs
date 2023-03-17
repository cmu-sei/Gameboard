
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ChallengeBonuses;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Structure;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public interface IScoringService
{
    Task<TeamChallengeScoreSummary> GetTeamChallengeScore(string challengeId);
    Task<TeamGameScoreSummary> GetTeamGameScore(string teamId);
    Task<ChallengeScoreSummary> GetChallengeScores(string challengeId);
}

internal class ScoringService : IScoringService
{
    private readonly IChallengeBonusStore _challengeBonusStore;
    private readonly IChallengeStore _challengeStore;
    private readonly IGameStore _gameStore;
    private readonly IMapper _mapper;
    private readonly ITeamService _teamService;

    public ScoringService(
        IChallengeBonusStore challengeBonusStore,
        IChallengeStore challengeStore,
        IGameStore gameStore,
        IMapper mapper,
        ITeamService teamService)
    {
        _challengeBonusStore = challengeBonusStore;
        _challengeStore = challengeStore;
        _gameStore = gameStore;
        _mapper = mapper;
        _teamService = teamService;
    }

    public Task<ChallengeScoreSummary> GetChallengeScores(string challengeId)
    {
        throw new System.NotImplementedException();
    }

    public async Task<TeamChallengeScoreSummary> GetTeamChallengeScore(string challengeId)
    {
        var challenge = await _challengeStore
            .List()
            .Include(c => c.Player)
            .FirstAsync(c => c.Id == challengeId);

        var bonuses = await _challengeBonusStore
            .List()
            .Where(b => b.ChallengeId == challengeId)
            .ToListAsync();

        var bonusScore = bonuses.Select(b => b.PointValue).Sum();
        var totalScore = CalculateScore(challenge.Points, bonuses.Select(b => b.PointValue));

        return new TeamChallengeScoreSummary
        {
            Challenge = new SimpleEntity { Id = challenge.Id, Name = challenge.Name },
            Team = new SimpleEntity { Id = challenge.TeamId, Name = challenge.Player.ApprovedName },
            TotalScore = totalScore,
            ScoreFromChallenge = challenge.Points,
            ScoreFromManualBonuses = bonusScore,
            ManualBonuses = _mapper.Map<IEnumerable<ManualChallengeBonusViewModel>>(bonuses)
        };
    }

    public async Task<TeamGameScoreSummary> GetTeamGameScore(string teamId)
    {
        var captain = await _teamService.ResolveCaptain(teamId);
        var game = await _gameStore
            .List()
            .SingleAsync(g => g.Id == captain.GameId);

        var challenges = await _challengeStore
            .List()
            .Include(c => c.AwardedManualBonuses)
            .Where(c => c.GameId == captain.GameId)
            .ToListAsync();

        var bonusPoints = challenges
            .SelectMany(c => c.AwardedManualBonuses.Select(b => b.PointValue))
            .Sum();

        var pointsFromChallengeScores = challenges.Select(c => c.Points).Sum();

        return new TeamGameScoreSummary
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            Team = new SimpleEntity { Id = captain.TeamId, Name = captain.ApprovedName },
            ChallengesScore = pointsFromChallengeScores,
            ManualBonusesScore = bonusPoints,
            TotalScore = pointsFromChallengeScores + bonusPoints,
            ChallengeScoreSummaries = challenges.Select(c =>
            {
                var bonusesSum = c.AwardedManualBonuses.Select(b => b.PointValue).Sum();

                return new TeamChallengeScoreSummary
                {
                    Challenge = new SimpleEntity { Id = c.Id, Name = c.Name },
                    Team = new SimpleEntity { Id = captain.TeamId, Name = captain.ApprovedName },
                    ScoreFromChallenge = c.Points,
                    ScoreFromManualBonuses = bonusesSum,
                    TotalScore = CalculateScore(c.Points, bonusesSum),
                    ManualBonuses = _mapper.Map<IEnumerable<ManualChallengeBonusViewModel>>(c.AwardedManualBonuses)
                };
            })
        };
    }

    internal double CalculateScore(double challengePoints, double manualPoints)
    {
        return CalculateScore(new double[] { challengePoints }, new double[] { manualPoints });
    }

    internal double CalculateScore(double challengePoints, IEnumerable<double> manualPoints)
    {
        return CalculateScore(new double[] { challengePoints }, manualPoints);
    }

    internal double CalculateScore(IEnumerable<double> challengesPoints, IEnumerable<double> manualPoints)
    {
        return challengesPoints.Sum() + manualPoints.Sum();
    }
}
