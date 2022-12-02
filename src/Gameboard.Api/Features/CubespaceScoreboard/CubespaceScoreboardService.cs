using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.CubespaceScoreboard;
using Gameboard.Api.Features.UnityGames;
using Microsoft.EntityFrameworkCore;

public class CubespaceScoreboardService : ICubespaceScoreboardService
{
    private static CubespaceScoreboardCache _scoreboardCache = new CubespaceScoreboardCache();
    private readonly IChallengeStore _challengeStore;
    private readonly IUnityGameService _unityGameService;

    public CubespaceScoreboardService(IChallengeStore challengeStore, IUnityGameService unityGameService)
    {
        _challengeStore = challengeStore;
        _unityGameService = unityGameService;
    }

    public async Task<CubespaceScoreboardState> GetScoreboard(CubespaceScoreboardRequestPayload payload)
    {
        try
        {
            // LOAD sponsors if needed
            if (_scoreboardCache.Sponsors.Count() == 0)
            {
                _scoreboardCache.Sponsors = await _challengeStore
                    .DbContext
                    .Sponsors
                    .AsNoTracking()
                    .Select(s => new CubespaceScoreboardSponsor
                    {
                        Name = s.Name,
                        LogoUri = s.Logo
                    })
                    .ToListAsync();
            }

            // LOAD game over time if needed
            if (_scoreboardCache.GameOverAt == null)
            {
                var cubespaceGame = await _challengeStore
                    .DbContext
                    .Games
                    .AsNoTracking()
                    .FirstAsync(g => g.Id == payload.CubespaceGameId);

                _scoreboardCache.GameOverAt = cubespaceGame.GameEnd.ToUnixTimeMilliseconds();
            }

            // LOAD player data (do this every time in case new teams enter the game
            // or players change their name)
            var players = await _challengeStore
                .DbContext
                .Players
                .Include(p => p.User)
                .AsNoTracking()
                .Where(p => p.GameId == payload.CubespaceGameId)
                .ToListAsync();

            var teams = players.Select(p => new CubespaceScoreboardTeam
            {
                Id = p.TeamId,
                Name = p.ApprovedName
            })
                .DistinctBy(p => p.Id)
                .ToList();
            var teamIds = teams.Select(t => t.Id);

            foreach (var team in teams)
            {
                // add the team to cache if they're not already there
                if (!_scoreboardCache.Teams.ContainsKey(team.Id))
                {
                    _scoreboardCache.Teams.Add(team.Id, new CubespaceScoreboardCacheTeam());
                }
            }

            // for now, doing this as two. will switch to client side filtered version
            // if this feels long, but it only happens once.
            //
            // RESOLVE a cubespace challenge for each team if we don't already have it
            var cachedTeamsWithCubespaceChallenge = _scoreboardCache.Teams.Values.Where(t => t.CubespaceChallenge != null).Count();
            if (cachedTeamsWithCubespaceChallenge < teams.Count())
            {
                var teamCubespaceChallenges = await ResolveChallenges(payload.CubespaceGameId, teamIds);
                foreach (var key in teamCubespaceChallenges.Keys)
                {
                    _scoreboardCache.Teams[key].CubespaceChallenge = teamCubespaceChallenges[key];
                }
            }

            var cachedTeamsWithDay1Challenge = _scoreboardCache.Teams.Values.Where(t => t.Day1Challenge != null).Count();
            if (cachedTeamsWithDay1Challenge < teams.Count())
            {
                var teamDay1Challenges = await ResolveDay1Challenges(payload.Day1GameId, payload.CubespaceGameId, teamIds);

                foreach (var key in teamDay1Challenges.Keys)
                {
                    _scoreboardCache.Teams[key].Day1Challenge = teamDay1Challenges[key];
                }
            }

            // if they don't have a day 1 challenge, they don't belong here
            teams = teams.Where(t => _scoreboardCache.Teams[t.Id].Day1Challenge != null).ToList();

            // ASSIGN players to teams
            foreach (var t in teams)
            {
                // load what we know about the team from cache to save calls
                var cachedTeam = _scoreboardCache.Teams[t.Id];

                // keep this fresh in case players change names/sponsors?
                var teamPlayers = players.Where(p => p.TeamId == t.Id);
                t.Players = teamPlayers
                    .Select(p => new CubespaceScoreboardPlayer
                    {
                        Id = p.Id,
                        Name = p.ApprovedName,
                        Sponsor = _scoreboardCache.Sponsors.First(sp => sp.LogoUri == p.Sponsor)
                    });

                // SET properties based on this data
                // all players are given the same rank by the scoring code in the challenge service
                t.Rank = teamPlayers.First().Rank;

                // if they don't have a day 1 challenge, that's not great, but let's keep going
                if (_scoreboardCache.Teams[t.Id].Day1Challenge != null)
                {
                    Console.WriteLine("No day 1 challenge for team", t.Id);
                    t.Day1Score = _scoreboardCache.Teams[t.Id].Day1Challenge.Score;
                    t.Day1Playtime = _scoreboardCache.Teams[t.Id].Day1Challenge.GetDuration();
                }

                if (cachedTeam.CubespaceChallenge == null)
                {
                    t.CubespaceStartTime = DateTimeOffset.MinValue.ToUnixTimeMilliseconds();
                    t.ScoredCodexes = new CubespaceScoreboardCodex[] { };
                }
                else
                {
                    t.CubespaceStartTime = cachedTeam.CubespaceChallenge.StartTime;

                    // build info about their scored codexes based on challenge events
                    // (do this every pull - these are codex events)
                    var challengeEvents = await _challengeStore
                        .DbContext
                        .ChallengeEvents
                        .AsNoTracking()
                        .Where(e => e.ChallengeId == cachedTeam.CubespaceChallenge.Id)
                        .ToListAsync();

                    // do this outside of EF because that gets weird fast
                    var eventRegex = _unityGameService.GetMissionCompleteEventRegex();
                    t.ScoredCodexes = challengeEvents
                        .Where(e => eventRegex.IsMatch(e.Text))
                        .Select(e =>
                        {
                            var match = eventRegex.Match(e.Text);

                            return new CubespaceScoreboardCodex
                            {
                                Codename = match.Groups["codename"].Value,
                                ScoredAt = e.Timestamp.ToUnixTimeMilliseconds()
                            };
                        })
                        .ToList();
                }
            }

            return new CubespaceScoreboardState
            {
                CubespaceGameId = payload.CubespaceGameId,
                Day1GameId = payload.Day1GameId,
                GameOverAt = _scoreboardCache.GameOverAt == null ? 0L : (long)_scoreboardCache.GameOverAt,
                Teams = teams.OrderBy(t => t.Rank)
            };
        }
        catch (Exception ex)
        {
            var stackTrace = new StackTrace(ex, true);
            var frame = stackTrace?.GetFrame(0);
            var lineNo = frame?.GetFileLineNumber();

            throw new Exception($"L{(lineNo != null ? lineNo : "NO LINE")} cache at this point:\n\n{JsonSerializer.Serialize(_scoreboardCache)}\n\n", ex);
        }
    }

    public void InvalidateScoreboardCache()
    {
        _scoreboardCache = new CubespaceScoreboardCache();
    }

    private async Task<IDictionary<string, CubespaceScoreboardCacheChallenge>> ResolveDay1Challenges(string day1GameId, string cubespaceGameId, IEnumerable<string> teamIds)
    {
        var players = await _challengeStore
            .DbContext
            .Players
            .AsNoTracking()
            .Include(p => p.User)
                .ThenInclude(u => u.Enrollments)
            .Where(p => p.GameId == day1GameId)
            .Where(p => p.User.Enrollments.Any(en => en.GameId == cubespaceGameId))
            .ToListAsync();

        var dict = new Dictionary<string, CubespaceScoreboardCacheChallenge>();

        foreach (var player in players.DistinctBy(c => c.TeamId))
        {
            // note that not all teams may have a cubespace challenge
            dict[player.TeamId] = CacheChallengeFromApiModel(player.Challenges.Single(c => c.GameId == day1GameId));
        }

        return dict;
    }

    private async Task<IDictionary<string, CubespaceScoreboardCacheChallenge>> ResolveChallenges(string gameId, IEnumerable<string> teamIds)
    {
        var challenges = await _challengeStore
            .DbSet
            .AsNoTracking()
            .Where(c => c.GameId == gameId)
            .ToListAsync();

        var dict = new Dictionary<string, CubespaceScoreboardCacheChallenge>();
        foreach (var challenge in challenges)
        {
            // note that not all teams may have a cubespace challenge
            dict[challenge.TeamId] = CacheChallengeFromApiModel(challenge);
        }

        return dict;
    }

    private CubespaceScoreboardCacheChallenge CacheChallengeFromApiModel(Gameboard.Api.Data.Challenge model)
        => new CubespaceScoreboardCacheChallenge
        {
            Id = model.Id,
            GameId = model.GameId,
            TeamId = model.TeamId,
            StartTime = model.StartTime.ToUnixTimeMilliseconds(),
            EndTime = model.EndTime.ToUnixTimeMilliseconds(),
            Score = (int)Math.Floor(model.Score)
        };

    private async Task<Gameboard.Api.Data.Game> ResolveGame(string gameId)
    {
        var candidateGames = await _challengeStore
            .DbContext
            .Games
            .AsNoTracking()
            .Where(g => g.Id != gameId)
            .ToListAsync();

        if (candidateGames.Count() != 1)
        {
            throw new GameResolutionFailure(candidateGames.Select(g => g.Id));
        }

        return candidateGames.First();
    }
}