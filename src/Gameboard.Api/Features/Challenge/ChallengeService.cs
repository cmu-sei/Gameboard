// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Scores;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services;

public class ChallengeService : _Service
{
    IChallengeStore Store { get; }
    IGameEngineService GameEngine { get; }

    private readonly IMemoryCache _localcache;
    private readonly ConsoleActorMap _actorMap;
    private readonly IGameStore _gameStore;
    private readonly IGuidService _guids;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJsonService _jsonService;
    private readonly LinkGenerator _linkGenerator;
    private readonly ILogger<ChallengeService> _logger;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly INowService _now;
    private readonly IPlayerStore _playerStore;
    private readonly IStore<Data.ChallengeSpec> _specStore;

    public ChallengeService(
        ILogger<ChallengeService> logger,
        IMapper mapper,
        CoreOptions options,
        IChallengeStore store,
        IStore<Data.ChallengeSpec> specStore,
        IGameEngineService gameEngine,
        IGameStore gameStore,
        IGuidService guids,
        IHttpContextAccessor httpContextAccessor,
        IJsonService jsonService,
        LinkGenerator linkGenerator,
        IMediator mediator,
        IMemoryCache localcache,
        INowService now,
        IPlayerStore playerStore,
        ConsoleActorMap actorMap
    ) : base(logger, mapper, options)
    {
        Store = store;
        GameEngine = gameEngine;
        _localcache = localcache;
        _actorMap = actorMap;
        _gameStore = gameStore;
        _guids = guids;
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _logger = logger;
        _mapper = mapper;
        _mediator = mediator;
        _jsonService = jsonService;
        _now = now;
        _playerStore = playerStore;
        _specStore = specStore;
    }

    public string BuildGraderUrl()
    {
        var request = _httpContextAccessor.HttpContext.Request;

        return string.Join('/', new string[]
        {
            _linkGenerator.GetUriByAction
            (
                _httpContextAccessor.HttpContext,
                "Grade",
                "Challenge",
                null,
                request.Scheme,
                request.Host,request.PathBase
            )
        });
    }

    public async Task<Challenge> GetOrCreate(NewChallenge model, string actorId, string graderUrl)
    {
        var entity = await Store.Load(model);

        if (entity is not null)
            return Mapper.Map<Challenge>(entity);

        return await Create(model, actorId, graderUrl);
    }

    public async Task<Challenge> Create(NewChallenge model, string actorId, string graderUrl)
    {
        var player = await _playerStore.Retrieve(model.PlayerId);

        var game = await _gameStore
            .List()
            .Include(g => g.Prerequisites)
            .Where(g => g.Id == player.GameId)
            .FirstOrDefaultAsync();

        if (await AtGamespaceLimit(game, player.TeamId))
            throw new GamespaceLimitReached();

        if ((await IsUnlocked(player, game, model.SpecId)).Equals(false))
            throw new ChallengeLocked();

        var lockkey = $"{player.TeamId}{model.SpecId}";
        var lockval = _guids.GetGuid();
        var locked = _localcache.GetOrCreate(lockkey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            return lockval;
        });

        if (locked != lockval)
            throw new ChallengeStartPending();

        var spec = await _specStore.Retrieve(model.SpecId);

        int playerCount = 1;
        if (game.AllowTeam)
            playerCount = await _playerStore.CountAsync(q => q.Where(p => p.TeamId == player.TeamId));

        try
        {
            // build and register
            var challenge = await BuildAndRegisterChallenge(model, spec, game, player, actorId, graderUrl, playerCount, model.Variant);

            await Store.Create(challenge);
            await Store.UpdateEtd(challenge.SpecId);

            return Mapper.Map<Challenge>(challenge);
        }
        // we need to catch here to allow cleanup in `finally`, but we want a complete rethrow
        // (and the compiler doesn't know we're doing this, so it needs to relax a little)
#pragma warning disable CA2200
        catch (Exception ex)
        {
            Logger.LogWarning($"Challenge registration failure: {ex.GetType().Name} -- {ex.Message}");
            ExceptionDispatchInfo.Capture(ex.InnerException == null ? ex : ex.InnerException).Throw();
            throw;
        }
        finally
        {
            _localcache.Remove(lockkey);
        }
    }

    private async Task<bool> IsUnlocked(Data.Player player, Data.Game game, string specId)
    {
        bool result = true;

        foreach (var prereq in game.Prerequisites.Where(p => p.TargetId == specId))
        {
            var condition = await Store.DbSet.AnyAsync
            (
                c =>
                    c.TeamId == player.TeamId &&
                    c.SpecId == prereq.RequiredId &&
                    c.Score >= prereq.RequiredScore
            );

            result &= condition;
        }

        return result;
    }

    public async Task<Challenge> Retrieve(string id)
    {
        var result = Mapper.Map<Challenge>(
            await Store.Load(id)
        );

        return result;
    }

    public async Task Delete(string id)
    {
        await Store.Delete(id);
        var entity = await Store.Load(id);
        await GameEngine.DeleteGamespace(entity);
    }

    public async Task<bool> UserIsTeamPlayer(string id, string subjectId)
    {
        var entity = await Store.Retrieve(id);

        return await Store.DbContext.Users.AnyAsync(u =>
            u.Id == subjectId &&
            u.Enrollments.Any(e => e.TeamId == entity.TeamId)
        );
    }

    public async Task<ChallengeSummary[]> List(SearchFilter model)
    {
        var q = Store.List(model.Term);

        // filter out challenge records with no state used to give starting score to player
        q = q.Where(p => p.Name != "_initialscore_" && p.State != null);
        q = q.OrderByDescending(p => p.LastSyncTime);
        q = q.Skip(model.Skip);

        if (model.Take > 0)
            q = q.Take(model.Take);

        // we have to resolve the query here, because we need to include player data as well
        // (and there's no direct model relation between challenge and the players in a team)
        var summaries = await Mapper.ProjectTo<ChallengeSummary>(q).ToArrayAsync();

        // resolve the players of the challenges that are coming back
        var teamIds = summaries.Select(s => s.TeamId);
        var teamPlayerMap = await _playerStore
            .List()
            .AsNoTracking()
            .Where(p => teamIds.Contains(p.TeamId))
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(g => g.Key, g => g);

        foreach (var summary in summaries)
        {
            var teamPlayers = teamPlayerMap[summary.TeamId];
            summary.Players = teamPlayers.Select(p => _mapper.Map<ChallengePlayer>(p));
        }

        return summaries;
    }

    public async Task<ChallengeOverview[]> ListByUser(string uid)
    {
        var q = Store.List(null);

        var userTeams = await Store.DbContext.Players
                .Where(p => p.UserId == uid && p.TeamId != null && p.TeamId != "")
                .Select(p => p.TeamId)
                .ToListAsync();

        q = q.Where(t => userTeams.Any(i => i == t.TeamId));

        DateTimeOffset recent = DateTimeOffset.UtcNow.AddDays(-1);
        q = q.Include(c => c.Player).Include(c => c.Game);
        q = q.Where(c => c.Game.GameEnd > recent);
        q = q.OrderByDescending(p => p.StartTime);

        return await Mapper.ProjectTo<ChallengeOverview>(q).ToArrayAsync();
    }

    public async Task<ArchivedChallenge[]> ListArchived(SearchFilter model)
    {
        var q = Store.DbContext.ArchivedChallenges.AsQueryable();

        if (model.Term.NotEmpty())
        {
            var term = model.Term.ToLower();
            q = q.Where(c =>
                c.Id.StartsWith(term) || // Challenge Id
                c.Tag.ToLower().StartsWith(term) || // Challenge Tag
                c.UserId.StartsWith(term) || // User Id
                c.Name.ToLower().Contains(term) || // Challenge Title
                c.PlayerName.ToLower().Contains(term) // Team Name (or indiv. Player Name)
            );
        }

        q = q.OrderByDescending(p => p.LastSyncTime);
        q = q.Skip(model.Skip);

        if (model.Take > 0)
            q = q.Take(model.Take);

        return await Mapper.ProjectTo<ArchivedChallenge>(q).ToArrayAsync();
    }

    public async Task<Challenge> Preview(NewChallenge model)
    {
        var entity = await Store.Load(model);

        if (entity is not null)
            return Mapper.Map<Challenge>(entity);

        var spec = await Store.DbContext.ChallengeSpecs.FindAsync(model.SpecId);
        var challenge = Mapper.Map<Data.Challenge>(spec);

        var result = Mapper.Map<Challenge>(challenge);
        GameEngineGameState state = new()
        {
            Markdown = spec.Text
        };
        Transform(state);
        result.State = state;
        return result;
    }

    public async Task SyncExpired()
    {
        var now = _now.Get();

        // a limitation of the current game engine architecture is that gamespaces can only be loaded one by one
        // (there's no multi-id signature), and we can't parallelize this because DbContext can't be used concurrently.
        // 
        // Just load them all, then sync one by one.
        var challenges = await Store.DbSet
            .Where(c => c.LastSyncTime < c.Player.SessionEnd && c.Player.SessionEnd < now)
            .ToArrayAsync();

        foreach (var challenge in challenges)
        {
            try
            {
                var state = await GameEngine.LoadGamespace(challenge);
                _actorMap.RemoveTeam(challenge.TeamId);
                Sync(challenge, state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"""Couldn't sync data for challenge {challenge.Id}.""");
            }
        }
    }

    private void Sync(Data.Challenge challenge, GameEngineGameState state)
        => Sync(new SyncEntry(challenge, state));

    private void Sync(params SyncEntry[] entries)
    {
        if (entries is null)
            throw new ArgumentNullException(nameof(entries));

        foreach (var entry in entries)
        {
            // TODO
            // this is currently awkward because the game state that comes back here has the team ID as the subjectId (because that's what we're passing to Topo - see 
            // GameEngine.RegisterGamespace). it's unclear whether topo cares what we pass as the players argument there, but since we're passing team ID 
            // there we need to NOT overwrite the playerId on the entity during the call to Map. Obviously, we could fix this by setting a rule on the map, 
            // but I'm leaving it here because this is the anomalous case.
            var playerId = entry.Challenge.PlayerId;
            Mapper.Map(entry.State, entry.Challenge);
            entry.Challenge.PlayerId = playerId;
        }
    }

    public async Task<Challenge> StartGamespace(string id, string actorId)
    {
        var challenge = await Store.Retrieve(id);
        var game = await Store.DbContext.Games.FindAsync(challenge.GameId);

        if (await AtGamespaceLimit(game, challenge.TeamId))
            throw new GamespaceLimitReached();

        challenge.Events.Add(new Data.ChallengeEvent
        {
            Id = _guids.GetGuid(),
            UserId = actorId,
            TeamId = challenge.TeamId,
            Timestamp = DateTimeOffset.UtcNow,
            Type = ChallengeEventType.GamespaceOn
        });

        var state = await GameEngine.StartGamespace(new GameEngineGamespaceStartRequest { ChallengeId = challenge.Id, GameEngineType = challenge.GameEngineType });
        Sync(challenge, state);

        return Mapper.Map<Challenge>(challenge);
    }

    public async Task<Challenge> StopGamespace(string id, string actorId)
    {
        var challenge = await Store.Retrieve(id);

        challenge.Events.Add(new Data.ChallengeEvent
        {
            Id = _guids.GetGuid(),
            UserId = actorId,
            TeamId = challenge.TeamId,
            Timestamp = DateTimeOffset.UtcNow,
            Type = ChallengeEventType.GamespaceOff
        });

        var state = await GameEngine.StopGamespace(challenge);
        Sync(challenge, state);

        return Mapper.Map<Challenge>(challenge);
    }

    public async Task<Challenge> Grade(GameEngineSectionSubmission model, string actorId)
    {
        var challenge = await Store.Retrieve(model.Id);

        challenge.Events.Add(new Data.ChallengeEvent
        {
            Id = _guids.GetGuid(),
            UserId = actorId,
            TeamId = challenge.TeamId,
            Timestamp = DateTimeOffset.UtcNow,
            Type = ChallengeEventType.Submission
        });

        var state = await GameEngine.GradeChallenge(challenge, model);
        Sync(challenge, state);

        // update the team score and award automatic bonuses
        await _mediator.Send(new UpdateTeamChallengeBaseScoreCommand(challenge.Id, challenge.Score));

        return Mapper.Map<Challenge>(challenge);
    }

    public async Task<Challenge> Regrade(string id)
    {
        // load and regrade
        var challenge = await Store.Retrieve(id);
        var state = await GameEngine.RegradeChallenge(challenge);
        Sync(challenge, state);

        // update the team score and award automatic bonuses
        await _mediator.Send(new UpdateTeamChallengeBaseScoreCommand(challenge.Id, challenge.Score));

        return Mapper.Map<Challenge>(challenge);
    }

    public async Task ArchivePlayerChallenges(Data.Player player)
    {
        // for this, we need to make sure that we're not cleaning up any challenges
        // that still belong to other members of the player's team (if they)
        // have any
        var candidateChallenges = await Store
            .List()
            .AsNoTracking()
            .Where(c => c.PlayerId == player.Id)
            .ToArrayAsync();

        var teamChallenges = await Store
            .List()
            .AsNoTracking()
            .Where(c => c.TeamId == player.TeamId && c.PlayerId != player.Id)
            .ToArrayAsync();

        var playerOnlyChallenges = candidateChallenges.Where(c => !teamChallenges.Any(tc => tc.Id == c.Id));

        await ArchiveChallenges(playerOnlyChallenges);
    }

    public async Task ArchiveTeamChallenges(string teamId)
    {
        Logger.LogInformation($"Archiving challenges for team {teamId}...");

        var challenges = await Store
            .List()
            .AsNoTracking()
            .Where(c => c.TeamId == teamId)
            .ToArrayAsync();

        Logger.LogInformation($"Cleaning up challenges for team {teamId}.");
        await ArchiveChallenges(challenges);
    }

    private async Task ArchiveChallenges(IEnumerable<Data.Challenge> challenges)
    {
        if (challenges == null || !challenges.Any())
            return;

        Logger.LogInformation($"Archiving {challenges.Count()} challenges.");
        var toArchiveIds = challenges.Select(c => c.Id).ToArray();
        var teamMemberMap = await Store
            .DbSet
            .AsNoTracking()
            .Include(c => c.Player)
            .Where(c => toArchiveIds.Contains(c.Id))
            .GroupBy(c => c.Player.TeamId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(c => c.Player.Id).AsEnumerable());

        var toArchiveTasks = challenges.Select(async challenge =>
        {
            var submissions = Array.Empty<GameEngineSectionSubmission>();

            // gamespace may be deleted in TopoMojo which would cause error and prevent reset
            try
            {
                submissions = Mapper.Map<GameEngineSectionSubmission[]>(await GameEngine.AuditChallenge(challenge));
                Logger.LogInformation($"Completing gamespace for challenge {challenge.Id}.");
                await GameEngine.CompleteGamespace(challenge);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Exception thrown during attempted cleanup of gamespace (type: {ex.GetType().Name}, message: {ex.Message})", ex);
            }

            var mappedChallenge = _mapper.Map<ArchivedChallenge>(challenge);
            mappedChallenge.Submissions = submissions;
            mappedChallenge.TeamMembers = teamMemberMap.ContainsKey(challenge.TeamId) ? teamMemberMap[challenge.TeamId].ToArray() : Array.Empty<string>();

            return mappedChallenge;
        }).ToArray();

        var toArchive = await Task.WhenAll(toArchiveTasks);

        // this is a backstoppy kind of thing - we aren't quite sure about the conditions under which this happens, but we've had
        // some stale challenges appear in the archive table and the real challenges table. if for whatever reason we're trying to
        // archive something that's already in the archive table, instead, delete it, replace it with the updated object
        var recordsAffected = await Store
            .DbContext
            .ArchivedChallenges
            .Where(c => toArchiveIds.Contains(c.Id))
            .ExecuteDeleteAsync();

        if (recordsAffected > 0)
            Logger.LogWarning($"While attempting to archive challenges (Ids: {string.Join(",", toArchiveIds)}) resulted in the deletion of ${recordsAffected} stale archive records.");

        Store.DbContext.ArchivedChallenges.AddRange(_mapper.Map<Data.ArchivedChallenge[]>(toArchive));
        await Store.DbContext.SaveChangesAsync();
    }

    public async Task<ConsoleSummary> GetConsole(ConsoleRequest model, bool observer)
    {
        var entity = await Store.Retrieve(model.SessionId);
        var challenge = Mapper.Map<Challenge>(entity);

        var thing = challenge.State.Vms.First();
        if (!challenge.State.Vms.Any(v => v.Name == model.Name))
            throw new ResourceNotFound<GameEngineVmState>("n/a", $"VMS for challenge {model.Name}");

        var console = await GameEngine.GetConsole(entity, model, observer);
        return console ?? throw new InvalidConsoleAction();
    }

    public async Task<List<ObserveChallenge>> GetChallengeConsoles(string gameId)
    {
        var q = Store.DbContext.Challenges
            .Where(c => c.GameId == gameId &&
                c.HasDeployedGamespace)
            .Include(c => c.Player)
            .OrderBy(c => c.Player.Name)
            .ThenBy(c => c.Name);
        var challenges = Mapper.Map<ObserveChallenge[]>(await q.ToArrayAsync());
        var result = new List<ObserveChallenge>();
        foreach (var challenge in challenges.Where(c => c.isActive))
        {
            challenge.Consoles = challenge.Consoles
                .Where(v => v.IsVisible)
                .ToArray();
            result.Add(challenge);
        }
        return result;
    }

    public ConsoleActor[] GetConsoleActors(string gameId)
    {
        return _actorMap.Find(gameId);
    }

    public ConsoleActor GetConsoleActor(string userId)
    {
        return _actorMap.FindActor(userId);
    }

    internal class SyncEntry
    {
        public Data.Challenge Challenge { get; private set; }
        public GameEngineGameState State { get; private set; }

        public SyncEntry(Data.Challenge challenge, GameEngineGameState state) =>
            (Challenge, State) = (challenge, state);
    }

    internal void Transform(GameEngineGameState state)
    {
        if (!string.IsNullOrWhiteSpace(state.Markdown))
            state.Markdown = state.Markdown.Replace("](/docs", $"]({Options.ChallengeDocUrl}docs");

        if (state.Challenge is not null && !string.IsNullOrWhiteSpace(state.Challenge.Text))
            state.Challenge.Text = state.Challenge.Text.Replace("](/docs", $"]({Options.ChallengeDocUrl}docs");
    }

    internal async Task<bool> AtGamespaceLimit(Data.Game game, string teamId)
    {
        int gamespaceCount = await Store.ChallengeGamespaceCount(teamId);
        int gamespaceLimit = game.IsCompetitionMode ? game.GamespaceLimitPerSession : 1;

        return gamespaceCount >= gamespaceLimit;
    }

    internal async Task<ConsoleActor> SetConsoleActor(ConsoleRequest model, string id, string name)
    {
        var entity = await Store.DbSet
            .Include(c => c.Player)
            .FirstOrDefaultAsync(c => c.Id == model.SessionId);

        return new ConsoleActor
        {
            UserId = id,
            UserName = name,
            PlayerName = entity.Player.Name,
            ChallengeName = entity.Name,
            ChallengeId = model.SessionId,
            GameId = entity.GameId,
            TeamId = entity.TeamId,
            VmName = model.Name,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    internal async Task<IEnumerable<GameEngineSectionSubmission>> Audit(string id)
    {
        var entity = await Store.Load(id);
        return await GameEngine.AuditChallenge(entity);
    }

    internal async Task<Data.Challenge> BuildAndRegisterChallenge
    (
        NewChallenge newChallenge,
        Data.ChallengeSpec spec,
        Data.Game game,
        Data.Player player,
        string actorUserId,
        string graderUrl,
        int playerCount,
        int variant
    )
    {
        var graderKey = _guids.GetGuid();
        var challenge = Mapper.Map<Data.Challenge>(newChallenge);
        Mapper.Map(spec, challenge);
        challenge.PlayerId = player.Id;
        challenge.TeamId = player.TeamId;
        challenge.GraderKey = graderKey.ToSha256();
        challenge.WhenCreated = _now.Get();

        var state = await GameEngine.RegisterGamespace(new GameEngineChallengeRegistration
        {
            Challenge = challenge,
            ChallengeSpec = spec,
            Game = game,
            GraderKey = graderKey,
            GraderUrl = graderUrl,
            Player = player,
            PlayerCount = playerCount,
            StartGamespace = newChallenge.StartGamespace,
            Variant = variant
        });

        Transform(state);

        // manually map here - we need the player object and other references to stay the same for
        // db add
        challenge.Id = state.Id;
        challenge.ExternalId = spec.ExternalId;
        challenge.HasDeployedGamespace = state.IsActive;
        challenge.State = _jsonService.Serialize(state);
        challenge.StartTime = state.StartTime;
        challenge.EndTime = state.EndTime;

        challenge.Events.Add(new Data.ChallengeEvent
        {
            Id = _guids.GetGuid(),
            UserId = actorUserId,
            TeamId = challenge.TeamId,
            Timestamp = DateTimeOffset.UtcNow,
            Type = ChallengeEventType.Started
        });

        return challenge;
    }
}
