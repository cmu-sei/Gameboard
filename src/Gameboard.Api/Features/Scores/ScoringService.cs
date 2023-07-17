
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ChallengeSpecs;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public interface IScoringService
{
    Task<GameScoringConfig> GetGameScoringConfig(string gameId);
    Task<TeamChallengeScoreSummary> GetTeamChallengeScore(string challengeId);
    Task<TeamGameScoreSummary> GetTeamGameScore(string teamId);
    Dictionary<string, int> ComputeTeamRanks(IEnumerable<TeamGameScoreSummary> teamScores);
}

internal class ScoringService : IScoringService
{
    private readonly IStore<ManualChallengeBonus> _challengeManualBonusStore;
    private readonly IChallengeStore _challengeStore;
    private readonly IStore<Data.ChallengeSpec> _challengeSpecStore;
    private readonly IGameStore _gameStore;
    private readonly IMapper _mapper;
    private readonly ITeamService _teamService;

    public ScoringService(
        IStore<ManualChallengeBonus> challengeManualBonusStore,
        IChallengeStore challengeStore,
        IStore<Data.ChallengeSpec> challengeSpecStore,
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

    public async Task<GameScoringConfig> GetGameScoringConfig(string gameId)
    {
        var game = await _gameStore.Retrieve(gameId);

        var challengeSpecs = await _challengeSpecStore
            .List()
            .AsNoTracking()
            .Where(s => s.GameId == gameId)
            .Include(s => s.Bonuses)
            .ToArrayAsync();

        // transform
        return new GameScoringConfig
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            ChallengeSpecScoringConfigs = challengeSpecs.Select(s =>
            {
                // note - we're currently assuming here that there's a max of one bonus per team, but
                // that doesn't have to necessarily be true forever
                var maxPossibleScore = (double)s.Points;

                if (s.Bonuses.Any(b => b.PointValue > 0))
                {
                    maxPossibleScore += s.Bonuses.OrderByDescending(b => b.PointValue).First().PointValue;
                }

                return new GameScoringConfigChallengeSpec
                {
                    ChallengeSpec = new SimpleEntity { Id = s.Id, Name = s.Description },
                    CompletionScore = s.Points,
                    PossibleBonuses = s.Bonuses.Select(b => _mapper.Map<GameScoringConfigChallengeBonus>(b)),
                    MaxPossibleScore = maxPossibleScore
                };
            })
        };
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
            return null;

        // get the specId so we can pull other competing challenges if there are bonuses
        var allChallenges = await _challengeStore
            .List()
            .Where(c => c.SpecId == challenge.SpecId)
            .ToArrayAsync();

        var spec = await _challengeSpecStore.Retrieve(challenge.SpecId);
        var unawardedBonuses = ResolveUnawardedBonuses(new Data.ChallengeSpec[] { spec }, allChallenges);

        return BuildTeamChallengeScoreSummary(challenge, spec, unawardedBonuses);
    }

    public async Task<TeamGameScoreSummary> GetTeamGameScore(string teamId)
    {
        var captain = await _teamService.ResolveCaptain(teamId, CancellationToken.None);
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
            .Include(s => s.Bonuses)
            .Where(spec => spec.GameId == captain.GameId)
            .ToListAsync();

        var unawardedBonuses = ResolveUnawardedBonuses(specs, challenges);
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

                return BuildTeamChallengeScoreSummary(challenge, spec, unawardedBonuses);
            })
        };
    }

    internal TeamChallengeScoreSummary BuildTeamChallengeScoreSummary(Data.Challenge challenge, Data.ChallengeSpec spec, IEnumerable<Data.ChallengeBonus> unawardedBonuses)
    {
        var manualBonuses = challenge == null ? new double[] { 0 } : challenge.AwardedManualBonuses.Select(b => b.PointValue).ToArray();
        var autoBonuses = challenge == null ? new double[] { 0 } : challenge.AwardedBonuses.Select(b => b.ChallengeBonus.PointValue).ToArray();
        var score = CalculateScore(challenge.Points, autoBonuses, manualBonuses);

        return new TeamChallengeScoreSummary
        {
            Challenge = challenge == null ? null : new SimpleEntity { Id = challenge.Id, Name = challenge.Name },
            Team = new SimpleEntity { Id = challenge.TeamId, Name = challenge.Player.ApprovedName },
            Spec = new SimpleEntity { Id = spec.Id, Name = spec.Name },
            Score = score,
            TimeElapsed = BuildChallengeTimeElapsed(challenge),
            Bonuses = _mapper.Map<IEnumerable<GameScoreAutoChallengeBonus>>(challenge.AwardedBonuses),
            ManualBonuses = _mapper.Map<ManualChallengeBonusViewModel[]>(challenge.AwardedManualBonuses),
            UnclaimedBonuses = _mapper.Map<IEnumerable<GameScoreAutoChallengeBonus>>(unawardedBonuses.Where(b => b.ChallengeSpecId == challenge.SpecId))
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

    internal IEnumerable<Data.ChallengeBonus> ResolveUnawardedBonuses(IEnumerable<Data.ChallengeSpec> specs, IEnumerable<Data.Challenge> challenges)
    {
        var awardedBonusIds = challenges.SelectMany(c => c.AwardedBonuses).Select(b => b.Id);

        return specs.SelectMany(s => s.Bonuses)
            .Where(b => !awardedBonusIds.Contains(b.Id))
            .ToArray();
    }
}
