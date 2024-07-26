
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Teams;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Scores;

public interface IScoringService
{
    Task<bool> CanAccessTeamScoreDetail(string teamId, CancellationToken cancellationToken);
    ChallengeResult GetChallengeResult(double score, double maxScore);
    Task<GameScoringConfig> GetGameScoringConfig(string gameId);
    Task<GameScore> GetGameScore(string gameId, CancellationToken cancellationToken);
    Task<TeamScore> GetTeamScore(string teamId, CancellationToken cancellationToken);
    Task<TeamChallengeScore> GetTeamChallengeScore(string challengeId);
    IDictionary<string, int?> GetTeamRanks(IEnumerable<TeamForRanking> teams);
}

internal class ScoringService : IScoringService
{
    private readonly IActingUserService _actingUserService;
    private readonly IMapper _mapper;
    private readonly INowService _now;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public ScoringService
    (
        IActingUserService actingUserService,
        IMapper mapper,
        INowService now,
        IStore store,
        ITeamService teamService)
    {
        _actingUserService = actingUserService;
        _mapper = mapper;
        _now = now;
        _store = store;
        _teamService = teamService;
    }

    public async Task<bool> CanAccessTeamScoreDetail(string teamId, CancellationToken cancellationToken)
    {
        var gameId = await _teamService.GetGameId(teamId, cancellationToken);
        var game = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == gameId)
            .SingleAsync(cancellationToken);

        if (game.AllowPublicScoreboardAccess && game.GameEnd.IsNotEmpty() && game.GameEnd <= _now.Get())
            return true;

        var currentUser = _actingUserService.Get();
        if (currentUser is not null)
        {
            if (currentUser.IsAdmin || currentUser.IsObserver)
                return true;

            var userTeamIds = await _teamService.GetUserTeamIds(currentUser.Id);
            if (userTeamIds.Contains(teamId))
                return true;
        }

        return false;
    }

    public ChallengeResult GetChallengeResult(double score, double maxScore)
    {
        if (score >= maxScore)
            return ChallengeResult.Success;

        if (score == 0)
            return ChallengeResult.None;

        return ChallengeResult.Partial;
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
                    maxPossibleScore += s.Bonuses.OrderByDescending(b => b.PointValue).First().PointValue;

                return new GameScoringConfigChallengeSpec
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    CompletionScore = s.Points,
                    PossibleBonuses = s.Bonuses
                        .Select(b =>
                        {
                            if (b is not ChallengeBonusCompleteSolveRank)
                                throw new NotImplementedException("Can't resolve subclasee of ChallengeBonus for game score config.");

                            return new GameScoringChallengeBonusSolveRank
                            {
                                Id = b.Id,
                                Description = b.Description,
                                PointValue = b.PointValue,
                                SolveRank = (b as ChallengeBonusCompleteSolveRank).SolveRank
                            };
                        })
                        .OrderByDescending(b => b.PointValue)
                            .ThenBy(b => b.Description),
                    MaxPossibleScore = maxPossibleScore,
                    SupportKey = s.Tag
                };
            }).
            OrderBy(config => config.Name)
        };
    }

    public async Task<TeamChallengeScore> GetTeamChallengeScore(string challengeId)
    {
        var challenge = await _store
            .WithNoTracking<Data.Challenge>()
            .Include(c => c.Game)
            .Include(c => c.Player)
            .Include(c => c.AwardedBonuses)
                .ThenInclude(b => b.ChallengeBonus)
            .Include(c => c.AwardedManualBonuses)
            .SingleOrDefaultAsync(c => c.Id == challengeId);

        if (challenge is null)
            return null;

        // get the specId so we can pull other competing challenges if there are bonuses
        var spec = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => s.Id == challenge.SpecId)
            .SingleAsync();

        var unawardedBonuses = await ResolveUnawardedBonuses(new string[] { spec.Id });
        return BuildTeamScoreChallenge(challenge, spec, unawardedBonuses);
    }

    public async Task<GameScore> GetGameScore(string gameId, CancellationToken cancellationToken)
    {
        var game = await _store.WithNoTracking<Data.Game>().SingleAsync(g => g.Id == gameId, cancellationToken);
        var scoringConfig = await GetGameScoringConfig(gameId);

        var players = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Sponsor)
            .Where(p => p.GameId == gameId)
            .Where(p => p.Mode == PlayerMode.Competition)
            .GroupBy(p => p.TeamId)
            .Select(g => new { TeamId = g.Key, Players = g.ToList() })
            .ToDictionaryAsync(g => g.TeamId, g => g.Players, cancellationToken);

        var captains = players.Keys
            .Select(teamId => _teamService.ResolveCaptain(players[teamId]))
            .ToDictionary(captain => captain.TeamId, captain => captain);

        // have to do these synchronously because we can't reuse the dbcontext
        // TODO: maybe a scoring service function that retrieves all at once and composes
        var teamScores = new Dictionary<string, TeamScore>();
        foreach (var teamId in captains.Keys)
        {
            teamScores.Add(teamId, await GetTeamScore(teamId, cancellationToken));
        }

        return new GameScore
        {
            Game = new GameScoreGameInfo
            {
                Id = game.Id,
                Name = game.Name,
                Specs = scoringConfig.Specs,
                IsTeamGame = game.IsTeamGame()
            },
            Teams = players
                .Keys
                .Select(teamId => teamScores[teamId])
                .OrderByDescending(t => t.OverallScore.TotalScore)
                    .ThenBy(t => t.CumulativeTimeMs)
        };
    }

    public async Task<TeamScore> GetTeamScore(string teamId, CancellationToken cancellationToken)
    {
        var players = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Sponsor)
            .Where(p => p.TeamId == teamId)
            .ToListAsync(cancellationToken);
        var captain = _teamService.ResolveCaptain(players);

        // hidden specs don't count toward score
        var specs = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Include(s => s.Bonuses)
            .Where(spec => spec.GameId == captain.GameId)
            .Where(spec => !spec.IsHidden)
            .ToListAsync(cancellationToken);
        var specIds = specs.Select(s => s.Id).ToArray();

        var game = await _store
            .WithNoTracking<Data.Game>()
            .SingleAsync(g => g.Id == captain.GameId, cancellationToken);

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
            .Where(c => specIds.Contains(c.SpecId))
            .ToListAsync(cancellationToken);

        var manualTeamBonuses = await _store
            .WithNoTracking<ManualTeamBonus>()
                .Include(t => t.EnteredByUser)
            .Where(b => b.TeamId == captain.TeamId)
            .ToListAsync(cancellationToken);

        var unawardedBonuses = await ResolveUnawardedBonuses(specs.Select(s => s.Id).ToArray());
        var manualChallengeBonusPoints = challenges.SelectMany(c => c.AwardedManualBonuses.Select(b => b.PointValue)).ToArray();
        var manualTeamBonusPoints = manualTeamBonuses.Select(b => b.PointValue).ToArray();
        var bonusPoints = challenges.SelectMany(c => c.AwardedBonuses.Select(b => b.ChallengeBonus.PointValue)).ToArray();
        var pointsFromChallenges = challenges.Select(c => (double)c.Score);
        var cumulativeTimeMs = challenges.Sum(c => c.Duration);

        // add the session end iff the team is currently playing
        var now = _now.Get();
        DateTimeOffset? teamSessionEnd = captain.SessionBegin > now && captain.SessionEnd < now ? captain.SessionEnd : null;
        var overallScore = CalculateScore(pointsFromChallenges, bonusPoints, manualTeamBonusPoints, manualChallengeBonusPoints, captain.AdvancedWithScore);

        return new TeamScore
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
            ManualBonuses = manualTeamBonuses.Select(b => new ManualTeamBonusViewModel
            {
                Id = b.Id,
                Description = b.Description,
                PointValue = b.PointValue,
                EnteredBy = new SimpleEntity { Id = b.EnteredByUserId, Name = b.EnteredByUser.ApprovedName },
                EnteredOn = b.EnteredOn,
                TeamId = b.TeamId
            }).OrderByDescending(b => b.PointValue),
            IsAdvancedToNextRound = captain.Advanced,
            OverallScore = overallScore,
            CumulativeTimeMs = cumulativeTimeMs,
            RemainingTimeMs = teamSessionEnd is null || teamSessionEnd < now ? null : (teamSessionEnd.Value - _now.Get()).TotalMilliseconds,
            Challenges = challenges.Select
            (
                c =>
                {
                    // every challenge should have a spec, but because specId is not on actual
                    // foreign key (for some reason), this is a bailout in case the spec
                    // is removed from the game
                    // (this also applies to challenges built from an "IsHidden" challenge spec)
                    var spec = specs.SingleOrDefault(s => s.Id == c.SpecId);
                    if (spec is null)
                        return null;

                    return BuildTeamScoreChallenge(c, spec, unawardedBonuses);
                }
            )
            .Where(c => c is not null)
            .OrderBy(c => c.Name)
        };
    }

    public IDictionary<string, int?> GetTeamRanks(IEnumerable<TeamForRanking> teams)
    {
        var scoreRank = 0;
        TeamForRanking lastScore = null;
        var retVal = new Dictionary<string, int?>();
        var ranked = teams
            .OrderByDescending(t => t.OverallScore).ThenBy(t => t.CumulativeTimeMs);

        foreach (var team in ranked)
        {
            if (team.SessionStart is null)
            {
                retVal.Add(team.TeamId, null);
                continue;
            }

            if (lastScore is null || team.OverallScore != lastScore.OverallScore || team.CumulativeTimeMs != lastScore.CumulativeTimeMs)
                scoreRank += 1;

            retVal.Add(team.TeamId, scoreRank);
            lastScore = team;
        }

        return retVal;
    }

    internal Task<ChallengeBonus[]> ResolveUnawardedBonuses(IEnumerable<string> specIds)
        => _store
            .WithNoTracking<ChallengeBonus>()
            .Where(b => specIds.Contains(b.ChallengeSpecId))
            .Where(b => !b.AwardedTo.Any())
            .ToArrayAsync();

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
            Result = GetChallengeResult(score.CompletionScore, challenge.Points),
            Score = score,
            TimeElapsed = CalculateTeamChallengeTimeElapsed(challenge),
            Bonuses = challenge.AwardedBonuses.Select(ab => new GameScoreAutoChallengeBonus
            {
                Id = ab.Id,
                Description = ab.ChallengeBonus.Description,
                PointValue = ab.ChallengeBonus.PointValue
            }),
            ManualBonuses = _mapper.Map<ManualChallengeBonusViewModel[]>(challenge.AwardedManualBonuses),
            UnclaimedBonuses = _mapper.Map<IEnumerable<GameScoreAutoChallengeBonus>>(unawardedBonuses.Where(b => b.ChallengeSpecId == challenge.SpecId))
        };
    }

    internal Score CalculateScore(double challengePoints, IEnumerable<double> bonusPoints, IEnumerable<double> manualTeamBonusPoints, IEnumerable<double> manualChallengeBonusPoints, double? advancedScore = null)
    {
        return CalculateScore(new double[] { challengePoints }, bonusPoints, manualTeamBonusPoints, manualChallengeBonusPoints, advancedScore);
    }

    internal Score CalculateScore(IEnumerable<double> challengesPoints, IEnumerable<double> bonusPoints, IEnumerable<double> manualTeamBonusPoints, IEnumerable<double> manualChallengeBonusPoints, double? advancedScore)
    {
        var solveScore = challengesPoints.Sum();
        var bonusScore = bonusPoints.Sum();
        var manualBonusScore = manualChallengeBonusPoints.Sum() + manualTeamBonusPoints.Sum();
        var addAdvancedScore = advancedScore is not null ? advancedScore.Value : 0;

        return new Score
        {
            AdvancedScore = advancedScore,
            CompletionScore = solveScore,
            BonusScore = bonusScore,
            ManualBonusScore = manualBonusScore,
            TotalScore = solveScore + bonusScore + manualBonusScore + addAdvancedScore
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
