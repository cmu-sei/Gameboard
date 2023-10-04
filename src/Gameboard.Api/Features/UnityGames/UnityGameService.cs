using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TopoMojo.Api.Client;

namespace Gameboard.Api.Features.UnityGames;

public interface IUnityGameService
{
    Task<Data.Challenge> AddChallenge(NewUnityChallenge newChallenge, User actor);
    Task<Data.ChallengeEvent> CreateMissionEvent(UnityMissionUpdate model, Api.User actor);
    Task<Data.Challenge> HasChallengeData(string gamespaceId);
    Task DeleteChallengeData(string gameId);
    bool IsUnityGame(Game game);
    bool IsUnityGame(Data.Game game);
    Regex GetMissionCompleteEventRegex();
    string GetMissionCompleteDefinitionString(string missionId);
    string GetUnityModeString();
    Task<string> UndeployGame(string gameId, string teamId);
}

internal class UnityGameService : _Service, IUnityGameService
{
    private readonly IGamebrainService _gamebrainService;
    private readonly IUnityStore _store;
    private readonly ITeamService _teamService;

    public UnityGameService(
            ILogger<UnityGameService> logger,
            IMapper mapper,
            CoreOptions options,
            IGamebrainService gamebrainService,
            ITeamService teamService,
            IUnityStore store,
            ConsoleActorMap actorMap
        ) : base(logger, mapper, options)
    {
        _gamebrainService = gamebrainService;
        _store = store;
        _teamService = teamService;
    }

    public async Task<Data.Challenge> AddChallenge(NewUnityChallenge newChallenge, User actor)
    {
        // each _team_ should only get one copy of the challenge, and by rule, that challenge must have the id
        // of the topo gamespace ID. If it's already in the DB, send them on their way with the challenge we've already got
        var existingChallenge = await _store.DbContext
            .Challenges
            .AsNoTracking()
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.Id == newChallenge.GamespaceId);

        if (existingChallenge != null)
        {
            return existingChallenge;
        }

        // otherwise, let's make some challenges
        var teamCaptain = await _teamService.ResolveCaptain(newChallenge.TeamId, CancellationToken.None);
        var challengeName = $"{teamCaptain.ApprovedName} vs. Cubespace";

        // load the spec associated with the game
        var challengeSpec = await _store.DbContext.ChallengeSpecs.FirstOrDefaultAsync(c => c.GameId == newChallenge.GameId);
        if (challengeSpec is null)
        {
            throw new SpecNotFound(newChallenge.GameId);
        }

        // we have to spoof topomojo data here. load the players, game, and related data.
        var teamPlayers = await _store.DbContext
            .Players
            .Where(p => p.TeamId == newChallenge.TeamId)
            .ToListAsync();

        var game = await this._store
            .DbContext
            .Games
            .FirstOrDefaultAsync(g => g.Id == newChallenge.GameId);

        if (game is null)
        {
            throw new ResourceNotFound<Data.Game>(newChallenge.GameId);
        }

        var state = new TopoMojo.Api.Client.GameState
        {
            Id = newChallenge.GameId,
            Name = game.Id,
            ManagerId = teamCaptain.Id,
            ManagerName = teamCaptain.ApprovedName,
            Markdown = game.GameMarkdown,
            Players = teamPlayers.Select(p => new TopoMojo.Api.Client.Player
            {
                GamespaceId = newChallenge.GamespaceId,
                SubjectId = p.Id,
                SubjectName = p.ApprovedName,
                Permission = p.IsManager ? TopoMojo.Api.Client.Permission.Manager : TopoMojo.Api.Client.Permission.None,
                IsManager = p.IsManager
            }).ToArray(),
            StartTime = game.GameStart,
            EndTime = game.GameEnd,
            ExpirationTime = game.GameStart.AddMinutes(game.SessionMinutes),
            WhenCreated = DateTimeOffset.UtcNow,
            Challenge = new ChallengeView()
            {
                Attempts = 1,
                LastScoreTime = DateTimeOffset.MinValue,
                MaxPoints = newChallenge.MaxPoints,
                Text = challengeName
            },
            IsActive = game.IsLive,
            Vms = newChallenge.Vms.Select(vm => new VmState
            {
                Id = vm.Id,
                Name = vm.Name,
                IsolationId = newChallenge.GamespaceId,
                IsVisible = true
            }).ToList()
        };

        var newChallengeEntity = new Data.Challenge
        {
            Id = newChallenge.GamespaceId,
            Name = $"{teamCaptain.ApprovedName} vs. Cubespace",
            GameId = challengeSpec.GameId,
            TeamId = newChallenge.TeamId,
            PlayerId = teamCaptain.Id,
            HasDeployedGamespace = true,
            SpecId = challengeSpec.Id,
            StartTime = DateTimeOffset.UtcNow,
            LastSyncTime = DateTimeOffset.UtcNow,
            State = JsonSerializer.Serialize(state),
            GraderKey = Guid.NewGuid().ToString("n").ToSha256(),
            Points = newChallenge.MaxPoints,
            Score = 0,
            Events = new List<Data.ChallengeEvent>
            {
                // an initial event to start the party
                new ChallengeEvent
                {
                    Id = Guid.NewGuid().ToString("n"),
                    UserId = actor.Id,
                    TeamId = newChallenge.TeamId,
                    Timestamp = DateTimeOffset.UtcNow,
                    Text = $"{teamCaptain.ApprovedName}'s has gathered their team and departed into CubeSpace...",
                    Type = ChallengeEventType.Started
                }
            },
            WhenCreated = DateTimeOffset.UtcNow,
        };

        await _store.DbContext.Challenges.AddAsync(newChallengeEntity);
        await _store.DbContext.SaveChangesAsync();

        return newChallengeEntity;
    }

    public async Task DeleteChallengeData(string gameId)
    {
        var challenges = await _store
            .DbContext
            .Challenges
            .Include(c => c.Events)
            .Where(c => c.GameId == gameId)
            .ToListAsync();

        if (challenges.Count == 0)
        {
            return;
        }

        _store.DbContext.Challenges.RemoveRange(challenges);
        _store.DbContext.ChallengeEvents.RemoveRange(challenges.SelectMany(c => c.Events));

        await _store.DbContext.SaveChangesAsync();
    }

    public async Task<Data.Challenge> HasChallengeData(string gamespaceId)
    {
        return await _store.HasChallengeData(gamespaceId);
    }

    public async Task<Data.ChallengeEvent> CreateMissionEvent(UnityMissionUpdate model, Api.User actor)
    {
        var unityMode = GetUnityModeString();
        var challengeCandidates = await _store.DbContext
            .Challenges
            .Include(c => c.Game)
            .Include(c => c.Events)
            .Include(c => c.Player)
            .Where(c => c.TeamId == model.TeamId && c.Game.Mode == unityMode)
            .ToListAsync();

        if (challengeCandidates.Count != 1)
        {
            throw new ChallengeResolutionFailure(model.TeamId, challengeCandidates.Select(c => c.Id));
        }

        // if we return null to the controller above, it interprets this as an "ok cool, we already have this one"
        // kind of thing
        var challenge = challengeCandidates.First();
        if (IsMissionComplete(challenge.Events, model.MissionId))
        {
            return null;
        }

        // record an event for this challenge
        var challengeEvent = new Data.ChallengeEvent
        {
            Id = Guid.NewGuid().ToString("n"),
            ChallengeId = challenge.Id,
            UserId = actor.Id,
            TeamId = model.TeamId,
            Text = $"{challenge.Player.ApprovedName}'s team has found the codex for {model.MissionName}! {GetMissionCompleteDefinitionString(model.MissionId)}",
            Type = ChallengeEventType.Submission,
            Timestamp = DateTimeOffset.UtcNow
        };
        challenge.Events.Add(challengeEvent);

        // also update the score of the challenge
        challenge.LastScoreTime = DateTimeOffset.UtcNow;
        challenge.Score += model.PointsScored;

        // save it up
        await _store.DbContext.SaveChangesAsync();

        // return (used to determine HTTP status code in an above controller)
        return challengeEvent;
    }

    public async Task<string> UndeployGame(string gameId, string teamId)
    {
        return await _gamebrainService.UndeployUnitySpace(gameId, teamId);
    }

    public bool IsUnityGame(Data.Game game) => game.Mode == GetUnityModeString();
    public bool IsUnityGame(Api.Game game) => game.Mode == GetUnityModeString();
    public string GetUnityModeString() => "unity";

    public Regex GetMissionCompleteEventRegex()
    {
        return new Regex(@"\[complete\:(?<codename>\S+)\]");
    }

    // if you change this, change `GetMissionCompleteEventRegex` above
    public string GetMissionCompleteDefinitionString(string missionId)
        => $"[complete:{missionId}]";

    internal bool IsMissionComplete(IEnumerable<Data.ChallengeEvent> events, string missionId)
        => events.Any(e => e.Text.Contains(GetMissionCompleteDefinitionString(missionId)));

    private Data.Player ResolveTeamCaptain(IEnumerable<Data.Player> players, NewUnityChallenge newChallenge)
    {
        if (!players.Any())
        {
            throw new CaptainResolutionFailure(newChallenge.TeamId);
        }

        // if the team has a captain (manager), yay
        // if they have too many, boo (pick one by name which is stupid but stupid things happen sometimes)
        // if they have none, congratulations to the player who called the API!
        var sortedPlayers = players.OrderBy(p => p.ApprovedName);
        var actingPlayer = players.First(p => p.Id == newChallenge.PlayerId);
        var captains = players.Where(p => p.IsManager);

        if (captains.Count() == 1)
        {
            return captains.First();
        }
        else if (captains.Count() > 1)
        {
            return captains.OrderBy(c => c.ApprovedName).First();
        }

        return actingPlayer;
    }
}
