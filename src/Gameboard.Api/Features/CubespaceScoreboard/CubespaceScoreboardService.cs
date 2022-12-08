using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.CubespaceScoreboard;
using Gameboard.Api.Features.UnityGames;
using Microsoft.EntityFrameworkCore;

public class CubespaceScoreboardService : ICubespaceScoreboardService
{
    public static CubespaceScoreboardCache _scoreboardCache = new CubespaceScoreboardCache();
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
            if (_scoreboardCache.GameClosesAt == null)
            {
                var cubespaceGame = await _challengeStore
                    .DbContext
                    .Games
                    .AsNoTracking()
                    .FirstAsync(g => g.Id == payload.CubespaceGameId);

                _scoreboardCache.GameClosesAt = cubespaceGame.GameEnd.ToUnixTimeMilliseconds();
            }

            // LOAD player data (do this every time in case new teams enter the game
            // or players change their name)
            var players = await _challengeStore
                .DbContext
                .Players
                .Include(p => p.User)
                .Include(p => p.Challenges)
                .AsNoTracking()
                .Where(p => p.GameId == payload.Day1GameId)
                .ToListAsync();

            var day1Teams = players.Select(p => new CubespaceScoreboardTeam
            {
                Id = p.TeamId,
                // ignore name for now - we'll resolve it later
                Day1Score = p.Score,
                Day1Playtime = p.Time,
            })
                .DistinctBy(team => team.Id)
                .ToList();

            // LOAD a map between day 1 and cubespace team ids if needed
            if (_scoreboardCache.Day1ToCubespaceTeamMap.Keys.Count() < day1Teams.Count())
            {
                _scoreboardCache.Day1ToCubespaceTeamMap = await GetTeamIdMap(players, payload.Day1GameId, payload.CubespaceGameId);
            }

            var day1TeamIds = day1Teams.Select(t => t.Id).ToList();

            foreach (var team in day1Teams)
            {
                // add the team to cache if they're not already there
                if (!_scoreboardCache.Teams.ContainsKey(team.Id))
                {
                    _scoreboardCache.Teams.Add(team.Id, new CubespaceScoreboardCacheTeam
                    {
                        Id = team.Id
                    });
                }
            }

            // for now, doing this as two. will switch to client side filtered version
            // if this feels long, but it only happens once.
            //
            // RESOLVE a cubespace challenge for each team if we don't already have it
            var cachedTeamsWithCubespaceChallenge = _scoreboardCache.Teams.Values.Where(t => t.CubespaceChallenge != null).Count();
            if (cachedTeamsWithCubespaceChallenge < day1Teams.Count())
            {
                Console.WriteLine("Retrieving cubespace challenge data for " + day1Teams.Count() + " teams.", LogFormat(day1TeamIds));
                var cubespaceTeamIds = day1TeamIds.Select(day1Id =>
                {
                    if (_scoreboardCache.Day1ToCubespaceTeamMap.ContainsKey(day1Id))
                    {
                        return _scoreboardCache.Day1ToCubespaceTeamMap[day1Id];
                    }

                    return null;
                });
                cubespaceTeamIds = cubespaceTeamIds.Where(id => id != null);

                Console.WriteLine("Their Cubespace team Ids are: ", LogFormat(cubespaceTeamIds));
                var teamCubespaceChallenges = await ResolveCubespaceChallenges(payload.CubespaceGameId, cubespaceTeamIds);
                Console.WriteLine($"Found {teamCubespaceChallenges.Keys.Count()} cubespace challenges", teamCubespaceChallenges);

                foreach (var key in teamCubespaceChallenges.Keys)
                {
                    var day1TeamId = _scoreboardCache.Day1ToCubespaceTeamMap
                        .Where(x => x.Value == key)
                        .Select(x => (KeyValuePair<string, string>?)x)
                        .FirstOrDefault();

                    if (day1TeamId == null)
                    {
                        Console.WriteLine($"Cubespace team {key} didn't play on day 1.");
                    }
                    else
                    {
                        _scoreboardCache.Teams[day1TeamId.Value.Key].CubespaceChallenge = teamCubespaceChallenges[key];
                    }
                }
            }

            // compute final scoreboard update by unifying cache with hot data
            foreach (var t in day1Teams)
            {
                // load what we know about the team from cache to save calls
                var cachedTeam = _scoreboardCache.Teams[t.Id];

                // keep this fresh in case players change names/sponsors?
                var teamPlayers = players.Where(p => p.TeamId == t.Id);
                t.Players = teamPlayers
                    .Select(p => new CubespaceScoreboardPlayer
                    {
                        Id = p.Id,
                        Name = p.User.ApprovedName,
                        Sponsor = _scoreboardCache.Sponsors.First(sp => sp.LogoUri == p.Sponsor)
                    });

                if (teamPlayers.Count() > 0)
                {
                    // default to a day 1 name 
                    var teamManager = teamPlayers.SingleOrDefault(p => p.TeamId == t.Id && p.IsManager);
                    t.Name = teamManager != null ? teamManager.ApprovedName : teamPlayers.First().ApprovedName;

                    // all players are given the same rank by the scoring code in the challenge service
                    t.Rank = teamPlayers.First().Rank;
                }

                if (cachedTeam.CubespaceChallenge == null)
                {
                    t.CubespaceStartTime = null;
                    t.ScoredCodexes = new CubespaceScoreboardCodex[] { };
                }
                else
                {
                    t.CubespaceTeamId = cachedTeam.CubespaceChallenge.TeamId;
                    t.CubespaceStartTime = cachedTeam.CubespaceChallenge.StartTime;
                    t.GameOverAt = cachedTeam.CubespaceChallenge.SessionEnd;
                    t.Name = cachedTeam.CubespaceChallenge.TeamName;

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

            Console.WriteLine($"Scoreboard cache at return point:\n\n{JsonSerializer.Serialize(_scoreboardCache)}");
            return new CubespaceScoreboardState
            {
                CubespaceGameId = payload.CubespaceGameId,
                Day1GameId = payload.Day1GameId,
                GameClosesAt = _scoreboardCache.GameClosesAt,
                Teams = day1Teams.OrderBy(t => t.Rank).ToList()
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Scoreboard cache at EXCEPTION point:\n\n{JsonSerializer.Serialize(_scoreboardCache)}", ex);
        }
    }

    public void InvalidateScoreboardCache()
    {
        _scoreboardCache = new CubespaceScoreboardCache();
    }

    private async Task<IDictionary<string, CubespaceScoreboardCacheChallenge>> ResolveCubespaceChallenges(string gameId, IEnumerable<string> teamIds)
    {
        var challenges = await _challengeStore
            .DbSet
            .AsNoTracking()
            .Include(c => c.Player)
            .Where(c => c.GameId == gameId)
            .Where(c => c.StartTime > DateTimeOffset.MinValue)
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
            TeamName = model.Player.ApprovedName,
            StartTime = model.StartTime.ToUnixTimeMilliseconds(),
            EndTime = model.EndTime.ToUnixTimeMilliseconds(),
            SessionEnd = model.Player.SessionEnd.ToUnixTimeMilliseconds(),
            Score = (int)Math.Floor(model.Score)
        };

    // ultimately returns a dict with the key  as a day 1 team id and the value as a cubespace team id
    private async Task<IDictionary<string, string>> GetTeamIdMap(IEnumerable<Gameboard.Api.Data.Player> day1Players, string day1GameId, string cubespaceGameId)
    {
        var day1UserIdPlayers = day1Players
            .Where(p => p.GameId == day1GameId)
            .ToDictionary
            (
                p => p.UserId,
                p => p
            );

        var cubespacePlayers = await _challengeStore
            .DbContext
            .Players
            .AsNoTracking()
            .Where(p => p.GameId == cubespaceGameId)
            .ToListAsync();

        var retVal = new Dictionary<string, string>();

        foreach (var userId in day1UserIdPlayers.Keys)
        {
            var day1TeamId = day1UserIdPlayers[userId].TeamId;
            var cubespaceTeamId = cubespacePlayers.SingleOrDefault(p => p.UserId == userId)?.TeamId;

            if (cubespaceGameId != null && !retVal.ContainsKey(day1TeamId))
            {
                retVal.Add(day1TeamId, cubespaceTeamId);
            }
        }

        return retVal;
    }

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

    private string LogFormat(object thing)
    {
        return JsonSerializer.Serialize(thing);
    }
}