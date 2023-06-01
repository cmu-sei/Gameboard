
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Common;
using Gameboard.Api.Features.Teams;
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
    private readonly IStore<ManualChallengeBonus> _challengeBonusStore;
    private readonly IChallengeStore _challengeStore;
    private readonly IChallengeSpecStore _challengeSpecStore;
    private readonly IGameStore _gameStore;
    private readonly IMapper _mapper;
    private readonly ITeamService _teamService;

    public ScoringService(
        IStore<ManualChallengeBonus> challengeBonusStore,
        IChallengeStore challengeStore,
        IChallengeSpecStore challengeSpecStore,
        IGameStore gameStore,
        IMapper mapper,
        ITeamService teamService)
    {
        _challengeBonusStore = challengeBonusStore;
        _challengeStore = challengeStore;
        _challengeSpecStore = challengeSpecStore;
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
            .FirstOrDefaultAsync(c => c.Id == challengeId);

        if (challenge == null)
        {
            return null;
        }

        var spec = await _challengeSpecStore.Retrieve(challenge.SpecId);
        var bonuses = await _mapper.ProjectTo<ManualChallengeBonusViewModel>(_challengeBonusStore
            .List()
            .Where(b => b.ChallengeId == challengeId))
            .ToListAsync();

        var bonusScore = bonuses.Select(b => b.PointValue).Sum();
        var totalScore = CalculateScore(challenge.Points, bonuses.Select(b => b.PointValue));

        return new TeamChallengeScoreSummary
        {
            Challenge = new SimpleEntity { Id = challenge.Id, Name = challenge.Name },
            Spec = new SimpleEntity { Id = spec.Id, Name = spec.Name },
            Team = new SimpleEntity { Id = challenge.TeamId, Name = challenge.Player.ApprovedName },
            TotalScore = totalScore,
            ScoreFromChallenge = challenge.Points,
            ScoreFromManualBonuses = bonusScore,
            ManualBonuses = bonuses
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
                .ThenInclude(b => b.EnteredByUser)
            .Where(c => c.GameId == captain.GameId)
            .ToListAsync();

        var specs = await _challengeSpecStore
            .List()
            .Where(spec => spec.GameId == captain.GameId)
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
            ChallengeScoreSummaries = specs.Select(spec =>
            {
                var challenge = challenges.FirstOrDefault(c => c.SpecId == spec.Id);
                var bonuses = challenge == null ? new ManualChallengeBonus[] { } : challenge.AwardedManualBonuses;
                var bonusesSum = challenge == null ? 0 : challenge.AwardedManualBonuses.Select(b => b.PointValue).Sum();
                var points = challenge == null ? 0 : challenge.Points;

                return new TeamChallengeScoreSummary
                {
                    Challenge = challenge == null ? null : new SimpleEntity { Id = challenge.Id, Name = challenge.Name },
                    Spec = new SimpleEntity { Id = spec.Id, Name = spec.Name },
                    Team = new SimpleEntity { Id = captain.TeamId, Name = captain.ApprovedName },
                    ScoreFromChallenge = points,
                    ScoreFromManualBonuses = bonusesSum,
                    TotalScore = CalculateScore(points, bonusesSum),
                    ManualBonuses = _mapper.Map<IEnumerable<ManualChallengeBonusViewModel>>(bonuses)
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
