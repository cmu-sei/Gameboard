
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public interface IScoringService
{
    Task<GameScoringConfig> GetGameScoringConfig(string gameId);
    Task<TeamChallengeScore> GetTeamChallengeScore(string challengeId);
    Task<GameScoreTeam> GetTeamGameScore(string teamId, int rank);
    Dictionary<string, int> ComputeTeamRanks(IEnumerable<GameScoreTeam> teamScores);
}

internal class ScoringService : IScoringService
{
    private readonly IChallengeStore _challengeStore;
    private readonly IStore<Data.ChallengeSpec> _challengeSpecStore;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public ScoringService
    (
        IChallengeStore challengeStore,
        IStore<Data.ChallengeSpec> challengeSpecStore,
        IMapper mapper,
        INowService now,
        IStore store,
        ITeamService teamService)
    {
        _challengeStore = challengeStore;
        _challengeSpecStore = challengeSpecStore;
        _mapper = mapper;
        _now = now;
        _store = store;
        _teamService = teamService;
    }

    public Dictionary<string, int> ComputeTeamRanks(IEnumerable<GameScoreTeam> teamScores)
    {
        var scoreRank = 0;
        GameScoreTeam lastScore = null;
        var rankedTeamScores = teamScores
            .OrderByDescending(s => s.OverallScore.TotalScore)
            .ThenBy(s => s.TotalTimeMs)
            .ToArray();
        var teamRanks = new Dictionary<string, int>();

        foreach (var teamScore in rankedTeamScores)
        {
            if (lastScore is null || teamScore.OverallScore.TotalScore != lastScore.OverallScore.TotalScore || teamScore.TotalTimeMs != lastScore.TotalTimeMs)
            {
                scoreRank += 1;
            }

            teamRanks.Add(teamScore.Team.Id, scoreRank);
            lastScore = teamScore;
        }

        return teamRanks;
    }

    public async Task<GameScoringConfig> GetGameScoringConfig(string gameId)
    {
        var game = await _store.WithNoTracking<Data.Game>().SingleAsync(g => g.Id == gameId);
        var challengeSpecs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.GameId == gameId)
            .Include(s => s.Bonuses)
            .ToArrayAsync(CancellationToken.None);

        // transform
        return new GameScoringConfig
        {
            Game = new SimpleEntity { Id = game.Id, Name = game.Name },
            Specs = challengeSpecs.Select(s =>
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
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    CompletionScore = s.Points,
                    PossibleBonuses = s.Bonuses
                        .Select(b => _mapper.Map<GameScoringConfigChallengeBonus>(b))
                        .OrderByDescending(b => b.PointValue)
                            .ThenBy(b => b.Description),
                    MaxPossibleScore = maxPossibleScore
                };
            }).
            OrderBy(config => config.Name)
        };
    }

    public async Task<TeamChallengeScore> GetTeamChallengeScore(string challengeId)
    {
        var challenge = await _challengeStore
            .List()
            .Include(c => c.Player)
            .Include(c => c.AwardedBonuses)
                .ThenInclude(b => b.ChallengeBonus)
            .Include(c => c.AwardedManualBonuses)
            .SingleOrDefaultAsync(c => c.Id == challengeId);

        if (challenge is null)
            return null;

        // get the specId so we can pull other competing challenges if there are bonuses
        var allChallenges = await _challengeStore
            .List()
            .Where(c => c.SpecId == challenge.SpecId)
            .ToArrayAsync();
        var spec = await _challengeSpecStore.Retrieve(challenge.SpecId);
        var unawardedBonuses = ResolveUnawardedBonuses(new Data.ChallengeSpec[] { spec }, allChallenges);

        return BuildTeamScoreChallenge(challenge, spec, unawardedBonuses);
    }

    public async Task<GameScoreTeam> GetTeamGameScore(string teamId, int rank)
    {
        var players = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Sponsor)
            .Where(p => p.TeamId == teamId)
            .ToListAsync();
        var captain = _teamService.ResolveCaptain(players);
        var game = await _store
            .WithNoTracking<Data.Game>()
            .SingleAsync(g => g.Id == captain.GameId);

        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Include(c => c.AwardedBonuses)
                .ThenInclude(b => b.ChallengeBonus)
                .AsSplitQuery()
            .Include(c => c.AwardedManualBonuses)
                .ThenInclude(b => b.EnteredByUser)
                .AsSplitQuery()
            .Where(c => c.GameId == captain.GameId)
            .Where(c => c.TeamId == teamId)
            .ToListAsync();

        var specs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Include(s => s.Bonuses)
            .Where(spec => spec.GameId == captain.GameId)
            .ToListAsync();

        var manualTeamBonuses = await _store
            .WithNoTracking<ManualTeamBonus>()
            .Where(b => b.TeamId == captain.TeamId)
            .ToListAsync();

        var unawardedBonuses = ResolveUnawardedBonuses(specs, challenges);
        var manualChallengeBonusPoints = challenges.SelectMany(c => c.AwardedManualBonuses.Select(b => b.PointValue)).ToArray();
        var manualTeamBonusPoints = manualTeamBonuses.Select(b => b.PointValue).ToArray();
        var bonusPoints = challenges.SelectMany(c => c.AwardedBonuses.Select(b => b.ChallengeBonus.PointValue)).ToArray();
        var pointsFromChallenges = challenges.Select(c => (double)c.Score);

        // add the session end iff the team is currently playing
        var now = _now.Get();
        DateTimeOffset? teamSessionEnd = captain.SessionBegin > now && captain.SessionEnd < now ? captain.SessionEnd : null;

        return new GameScoreTeam
        {
            Team = new SimpleEntity { Id = captain.TeamId, Name = captain.ApprovedName },
            Players = players.Select(p => new PlayerWithSponsor
            {
                Id = p.Id,
                Name = p.ApprovedName,
                Sponsor = new SimpleSponsor
                {
                    Id = p.Sponsor.Id,
                    Name = p.Sponsor.Name,
                    Logo = p.Sponsor.Logo
                }
            }).ToArray(),
            LiveSessionEnds = teamSessionEnd,
            ManualTeamBonuses = Array.Empty<ManualTeamBonus>(),
            OverallScore = CalculateScore(pointsFromChallenges, bonusPoints, manualTeamBonusPoints, manualChallengeBonusPoints),
            Rank = rank,
            TotalTimeMs = captain.Time,
            Challenges = challenges.Select
            (
                c =>
                {
                    // every challenge should have a spec, but because specId is not on actual
                    // foreign key (for some reason), this is a bailout in case the spec
                    // is removed from the game
                    var spec = specs.SingleOrDefault(s => s.Id == c.SpecId);
                    if (spec is null)
                        return null;

                    return BuildTeamScoreChallenge(c, spec, unawardedBonuses);
                }
            )
        };
    }

    internal IEnumerable<Data.ChallengeBonus> ResolveUnawardedBonuses(IEnumerable<Data.ChallengeSpec> specs, IEnumerable<Data.Challenge> challenges)
    {
        var awardedBonusIds = challenges.SelectMany(c => c.AwardedBonuses).Select(b => b.ChallengeBonusId);

        return specs
            .SelectMany(s => s.Bonuses)
            .Where(b => !awardedBonusIds.Contains(b.Id))
            .ToArray();
    }

    internal TeamChallengeScore BuildTeamScoreChallenge(Data.Challenge challenge, Data.ChallengeSpec spec, IEnumerable<Data.ChallengeBonus> unawardedBonuses)
    {
        var manualChallengeBonuses = challenge == null ? new double[] { 0 } : challenge.AwardedManualBonuses.Select(b => b.PointValue).ToArray();
        var autoBonuses = challenge == null ? new double[] { 0 } : challenge.AwardedBonuses.Select(b => b.ChallengeBonus.PointValue).ToArray();
        var score = CalculateScore(challenge.Score, autoBonuses, Array.Empty<double>(), manualChallengeBonuses);

        return new TeamChallengeScore
        {
            Id = challenge.Id,
            Name = spec.Name,
            SpecId = spec.Id,
            Result = challenge.Result,
            Score = score,
            TimeElapsed = CalculateTeamChallengeTimeElapsed(challenge),
            Bonuses = challenge.AwardedBonuses.Select(ab => new GameScoreAutoChallengeBonus
            {
                Id = ab.Id,
                Description = ab.ChallengeBonus.Description,
                PointValue = ab.ChallengeBonus.PointValue
            }),
            ManualChallengeBonuses = _mapper.Map<ManualChallengeBonusViewModel[]>(challenge.AwardedManualBonuses),
            UnclaimedBonuses = _mapper.Map<IEnumerable<GameScoreAutoChallengeBonus>>(unawardedBonuses.Where(b => b.ChallengeSpecId == challenge.SpecId))
        };
    }

    internal Score CalculateScore(double challengePoints, IEnumerable<double> bonusPoints, IEnumerable<double> manualTeamBonusPoints, IEnumerable<double> manualChallengeBonusPoints)
    {
        return CalculateScore(new double[] { challengePoints }, bonusPoints, manualTeamBonusPoints, manualChallengeBonusPoints);
    }

    internal Score CalculateScore(IEnumerable<double> challengesPoints, IEnumerable<double> bonusPoints, IEnumerable<double> manualTeamBonusPoints, IEnumerable<double> manualChallengeBonusPoints)
    {
        var solveScore = challengesPoints.Sum();
        var bonusScore = bonusPoints.Sum();
        var manualBonusScore = manualChallengeBonusPoints.Sum() + manualTeamBonusPoints.Sum();

        return new Score
        {
            CompletionScore = solveScore,
            BonusScore = bonusScore,
            ManualBonusScore = manualBonusScore,
            TotalScore = solveScore + bonusScore + manualBonusScore
        };
    }

    internal TimeSpan? CalculateTeamChallengeTimeElapsed(Data.Challenge challenge)
    {
        if (challenge.StartTime.IsEmpty())
            return null;

        if (challenge.Result == ChallengeResult.Success)
            return challenge.LastScoreTime - challenge.StartTime;

        return challenge.EndTime - challenge.StartTime;
    }
}
