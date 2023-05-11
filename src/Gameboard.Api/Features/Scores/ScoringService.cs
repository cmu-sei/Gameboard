
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Structure;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public interface IScoringService
{
    Task<TeamChallengeScoreSummary> GetTeamChallengeScore(string challengeId);
    Task<TeamGameScoreSummary> GetTeamGameScore(string teamId);
    Dictionary<string, int> ComputeTeamRanks(IEnumerable<TeamGameScoreSummary> teamScores);
}

internal class ScoringService : IScoringService
{
    private readonly IStore<ManualChallengeBonus> _challengeManualBonusStore;
    private readonly IChallengeStore _challengeStore;
    private readonly IChallengeSpecStore _challengeSpecStore;
    private readonly IGameStore _gameStore;
    private readonly IMapper _mapper;
    private readonly ITeamService _teamService;

    public ScoringService(
        IStore<ManualChallengeBonus> challengeManualBonusStore,
        IChallengeStore challengeStore,
        IChallengeSpecStore challengeSpecStore,
        IGameStore gameStore,
        IMapper mapper,
        ITeamService teamService)
    {
        _challengeManualBonusStore = challengeManualBonusStore;
        _challengeStore = challengeStore;
        _challengeSpecStore = challengeSpecStore;
        _gameStore = gameStore;
        _mapper = mapper;
        _teamService = teamService;
    }

    public async Task<TeamChallengeScoreSummary> GetTeamChallengeScore(string challengeId)
    {
        var challenge = await _challengeStore
            .List()
            .Include(c => c.Player)
            .Include(c => c.AwardedBonuses)
                .ThenInclude(b => b.ChallengeBonus)
            .Include(c => c.AwardedManualBonuses)
            .FirstOrDefaultAsync(c => c.Id == challengeId);

        if (challenge == null)
        {
            return null;
        }

        var spec = await _challengeSpecStore.Retrieve(challenge.SpecId);

        return BuildTeamChallengeScoreSummary(challenge, spec);
    }

    public async Task<TeamGameScoreSummary> GetTeamGameScore(string teamId)
    {
        var captain = await _teamService.ResolveCaptain(teamId);
        var game = await _gameStore
            .List()
            .SingleAsync(g => g.Id == captain.GameId);

        var challenges = await _challengeStore
            .List()
            .Include(c => c.AwardedBonuses)
                .ThenInclude(b => b.ChallengeBonus)
            .Include(c => c.AwardedManualBonuses)
                .ThenInclude(b => b.EnteredByUser)
            .Where(c => c.GameId == captain.GameId)
            .ToListAsync();

        var specs = await _challengeSpecStore
            .List()
            .Where(spec => spec.GameId == captain.GameId)
            .ToListAsync();

        var manualBonusPoints = challenges.SelectMany(c => c.AwardedManualBonuses.Select(b => b.PointValue));
        var bonusPoints = challenges.SelectMany(c => c.AwardedBonuses.Select(b => b.ChallengeBonus.PointValue));
        var pointsFromChallenges = challenges.Select(c => (double)c.Points);

        return new TeamGameScoreSummary
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            Team = new SimpleEntity { Id = captain.TeamId, Name = captain.ApprovedName },
            Score = CalculateScore(pointsFromChallenges, bonusPoints, manualBonusPoints),
            ChallengeScoreSummaries = specs.Select(spec =>
            {
                var challenge = challenges.FirstOrDefault(c => c.SpecId == spec.Id);

                return BuildTeamChallengeScoreSummary(challenge, spec);
            })
        };
    }

    internal TeamChallengeScoreSummary BuildTeamChallengeScoreSummary(Data.Challenge challenge, Data.ChallengeSpec spec)
    {
        var manualBonuses = challenge == null ? new double[] { 0 } : challenge.AwardedManualBonuses.Select(b => b.PointValue);
        var autoBonuses = challenge == null ? new double[] { 0 } : challenge.AwardedBonuses.Select(b => b.ChallengeBonus.PointValue);
        var score = CalculateScore(challenge.Points, autoBonuses, manualBonuses);

        return new TeamChallengeScoreSummary
        {
            Challenge = challenge == null ? null : new SimpleEntity { Id = challenge.Id, Name = challenge.Name },
            Team = new SimpleEntity { Id = challenge.TeamId, Name = challenge.Player.ApprovedName },
            Spec = new SimpleEntity { Id = spec.Id, Name = spec.Name },
            Score = score,
            TimeElapsed = BuildChallengeTimeElapsed(challenge),
            Bonuses = _mapper.Map<IEnumerable<GameScoreAwardedChallengeBonus>>(challenge.AwardedBonuses),
            ManualBonuses = _mapper.Map<ManualChallengeBonusViewModel[]>(challenge.AwardedManualBonuses)
        };
    }

    internal Nullable<TimeSpan> BuildChallengeTimeElapsed(Data.Challenge c)
    {
        if (c == null || c.EndTime == DateTimeOffset.MinValue || c.StartTime == DateTimeOffset.MinValue)
            return null;

        return c.EndTime - c.StartTime;
    }

    internal Score CalculateScore(double challengePoints, IEnumerable<double> bonusPoints, IEnumerable<double> manualBonusPoints)
    {
        return CalculateScore(new double[] { challengePoints }, bonusPoints, manualBonusPoints);
    }

    internal Score CalculateScore(IEnumerable<double> challengesPoints, IEnumerable<double> bonusPoints, IEnumerable<double> manualBonusPoints)
    {
        var solveScore = challengesPoints.Sum();
        var bonusScore = bonusPoints.Sum();
        var manualBonusScore = manualBonusPoints.Sum();

        return new Score
        {
            CompletionScore = solveScore,
            BonusScore = bonusScore,
            ManualBonusScore = manualBonusScore,
            TotalScore = solveScore + bonusScore + manualBonusScore
        };
    }

    public Dictionary<string, int> ComputeTeamRanks(IEnumerable<TeamGameScoreSummary> teamScores)
    {
        // have to do these synchronously because we can't reuse the dbcontext
        // TODO: maybe a scoring service function that retrieves all at once and composes
        var scoreRank = 0;
        var lastScore = 0;
        var rankedTeamScores = teamScores.OrderBy(s => s.Score.TotalScore).ToArray();
        var teamRanks = new Dictionary<string, int>();

        foreach (var teamScore in rankedTeamScores)
        {
            if (teamScore.Score.TotalScore != lastScore)
            {
                scoreRank += 1;
            }
            teamRanks.Add(teamScore.Team.Id, scoreRank);
        }

        return teamRanks;
    }
}
