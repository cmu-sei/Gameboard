// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Scores;
using MediatR;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Services;

public partial class ChallengeService : _Service
{
    private readonly ConsoleActorMap _actorMap;
    private readonly IChallengeStore _challengeStore;
    private readonly IStore<Data.ChallengeSpec> _challengeSpecStore;
    private readonly IGameEngineService _gameEngine;
    private readonly IGameStore _gameStore;
    private readonly IGuidService _guids;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJsonService _jsonService;
    private readonly LinkGenerator _linkGenerator;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly IMemoryCache _memCache;
    private readonly INowService _now;
    private readonly IPlayerStore _playerStore;
    private readonly IPracticeChallengeScoringListener _practiceChallengeScoringListener;
    private readonly IStore<Data.ChallengeSpec> _specStore;
    private readonly IChallengeDocsService _challengeDocsService;
    private readonly IChallengeSyncService _challengeSyncService;
    private readonly IStore _store;
    private readonly ITeamService _teamService;

    public ChallengeService(
        ConsoleActorMap actorMap,
        CoreOptions coreOptions,
        IChallengeStore challengeStore,
        IChallengeDocsService challengeDocsService,
        IStore<Data.ChallengeSpec> specStore,
        IChallengeSyncService challengeSyncService,
        IGameEngineService gameEngine,
        IGameStore gameStore,
        IGuidService guids,
        IHttpContextAccessor httpContextAccessor,
        IJsonService jsonService,
        LinkGenerator linkGenerator,
        ILogger<ChallengeService> logger,
        IMapper mapper,
        IMediator mediator,
        IMemoryCache memCache,
        INowService now,
        IPlayerStore playerStore,
        IPracticeChallengeScoringListener practiceChallengeScoringListener,
        IStore store,
        ITeamService teamService
    ) : base(logger, mapper, coreOptions)
    {
        _actorMap = actorMap;
        _challengeStore = challengeStore;
        _challengeSpecStore = specStore;
        _challengeDocsService = challengeDocsService;
        _challengeSyncService = challengeSyncService;
        _gameEngine = gameEngine;
        _gameStore = gameStore;
        _guids = guids;
        _httpContextAccessor = httpContextAccessor;
        _jsonService = jsonService;
        _linkGenerator = linkGenerator;
        _mapper = mapper;
        _mediator = mediator;
        _memCache = memCache;
        _now = now;
        _playerStore = playerStore;
        _practiceChallengeScoringListener = practiceChallengeScoringListener;
        _specStore = specStore;
        _store = store;
        _teamService = teamService;
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
        var entity = await _challengeStore.Load(model);

        if (entity is not null)
            return Mapper.Map<Challenge>(entity);

        return await Create(model, actorId, graderUrl, CancellationToken.None);
    }

    public async Task<Challenge> Create(NewChallenge model, string actorId, string graderUrl, CancellationToken cancellationToken)
    {
        var player = await _playerStore.Retrieve(model.PlayerId);

        var game = await _gameStore
            .List()
            .Include(g => g.Prerequisites)
            .Where(g => g.Id == player.GameId)
            .FirstOrDefaultAsync(cancellationToken);

        if (await _teamService.IsAtGamespaceLimit(player.TeamId, game, cancellationToken))
            throw new GamespaceLimitReached(game.Id, player.TeamId);

        if ((await IsUnlocked(player, game, model.SpecId)).Equals(false))
            throw new ChallengeLocked();

        var lockkey = $"{player.TeamId}{model.SpecId}";
        var lockval = _guids.GetGuid();
        var locked = _memCache.GetOrCreate(lockkey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            return lockval;
        });

        if (locked != lockval)
            throw new ChallengeStartPending();

        var spec = await _challengeSpecStore.Retrieve(model.SpecId);

        int playerCount = 1;
        if (game.AllowTeam)
        {
            playerCount = await _playerStore.CountAsync(q => q.Where(p => p.TeamId == player.TeamId));
        }

        try
        {
            var challenge = await BuildAndRegisterChallenge(model, spec, game, player, actorId, graderUrl, playerCount, model.Variant);

            await _challengeStore.Create(challenge);
            await _challengeStore.UpdateEtd(challenge.SpecId);

            return Mapper.Map<Challenge>(challenge);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(message: $"Challenge registration failure: {ex.GetType().Name} -- {ex.Message}");
            ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
            throw;
        }
        finally
        {
            _memCache.Remove(lockkey);
        }
    }

    private async Task<bool> IsUnlocked(Data.Player player, Data.Game game, string specId)
    {
        var result = true;

        foreach (var prereq in game.Prerequisites.Where(p => p.TargetId == specId))
        {
            var condition = await _challengeStore.DbSet.AnyAsync
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
            await _challengeStore.Load(id)
        );

        return result;
    }

    public async Task Delete(string id)
    {
        await _challengeStore.Delete(id);
        var entity = await _challengeStore.Load(id);
        await _gameEngine.DeleteGamespace(entity);
    }

    public async Task<bool> UserIsTeamPlayer(string id, string subjectId)
    {
        var entity = await _challengeStore.Retrieve(id);

        return await _challengeStore.DbContext.Users.AnyAsync(u =>
            u.Id == subjectId &&
            u.Enrollments.Any(e => e.TeamId == entity.TeamId)
        );
    }

    public async Task<ChallengeSummary[]> List(SearchFilter model = null)
    {
        var q = _challengeStore.List(model?.Term?.Trim() ?? null);

        // filter out challenge records with no state used to give starting score to player
        q = q.Where(p => p.Name != "_initialscore_" && p.State != null);
        q = q.OrderByDescending(p => p.LastSyncTime);
        q = q.Skip(model?.Skip ?? 0);

        if (model?.Take > 0)
        {
            q = q.Take(model.Take);
        }

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
            if (teamPlayerMap.ContainsKey(summary.TeamId))
            {
                var teamPlayers = teamPlayerMap[summary.TeamId];
                summary.Players = teamPlayers.Select(p => _mapper.Map<ChallengePlayer>(p));
            }
            else
            {
                summary.Players = Array.Empty<ChallengePlayer>();
            }
        }

        return summaries;
    }

    public async Task<ChallengeOverview[]> ListByUser(string uid)
    {
        var q = _challengeStore.List(null);

        var userTeams = await _challengeStore.DbContext.Players
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
        var q = _challengeStore.DbContext.ArchivedChallenges.AsQueryable();

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
        var entity = await _challengeStore.Load(model);

        if (entity is not null)
            return Mapper.Map<Challenge>(entity);

        var spec = await _challengeStore.DbContext.ChallengeSpecs.FindAsync(model.SpecId);
        var challenge = Mapper.Map<Data.Challenge>(spec);

        var result = Mapper.Map<Challenge>(challenge);
        GameEngineGameState state = new() { Markdown = spec.Text };
        Transform(state);
        result.State = state;
        return result;
    }

    public async Task<Challenge> StartGamespace(string id, string actorId, CancellationToken cancellationToken)
    {
        var challenge = await _challengeStore.Retrieve(id);
        var game = await _challengeStore.DbContext.Games.FindAsync(challenge.GameId);

        if (await _teamService.IsAtGamespaceLimit(challenge.TeamId, game, cancellationToken))
            throw new GamespaceLimitReached(game.Id, challenge.TeamId);

        challenge.Events.Add(new ChallengeEvent
        {
            Id = _guids.GetGuid(),
            UserId = actorId,
            TeamId = challenge.TeamId,
            Timestamp = DateTimeOffset.UtcNow,
            Type = ChallengeEventType.GamespaceOn
        });

        var state = await _gameEngine.StartGamespace(new GameEngineGamespaceStartRequest { ChallengeId = challenge.Id, GameEngineType = challenge.GameEngineType });
        await _challengeSyncService.Sync(challenge, state, cancellationToken);

        return Mapper.Map<Challenge>(challenge);
    }

    public async Task<Challenge> StopGamespace(string id, string actorId)
    {
        var challenge = await _challengeStore.Retrieve(id);

        challenge.Events.Add(new ChallengeEvent
        {
            Id = _guids.GetGuid(),
            UserId = actorId,
            TeamId = challenge.TeamId,
            Timestamp = DateTimeOffset.UtcNow,
            Type = ChallengeEventType.GamespaceOff
        });

        var state = await _gameEngine.StopGamespace(challenge);
        await _challengeSyncService.Sync(challenge, state, CancellationToken.None);

        return Mapper.Map<Challenge>(challenge);
    }

    public async Task<Challenge> Grade(GameEngineSectionSubmission model, string actorId)
    {
        var challenge = await _challengeStore.Retrieve(model.Id);

        challenge.Events.Add(new ChallengeEvent
        {
            Id = _guids.GetGuid(),
            UserId = actorId,
            TeamId = challenge.TeamId,
            Timestamp = _now.Get(),
            Type = ChallengeEventType.Submission
        });

        // record the score so we can update team score if needed
        double currentScore = challenge.Score;

        var state = await _gameEngine.GradeChallenge(challenge, model);
        await _challengeSyncService.Sync(challenge, state, CancellationToken.None);

        // update the team score and award automatic bonuses
        await _mediator.Send(new UpdateTeamChallengeBaseScoreCommand(challenge.Id, challenge.Score));

        if (challenge.PlayerMode == PlayerMode.Practice)
        {
            if (challenge.Score >= challenge.Points)
            {
                // in the practice area, we proactively end their session if they complete the challenge
                await _practiceChallengeScoringListener.NotifyChallengeScored(challenge, CancellationToken.None);
            }

            // also for the practice area:
            // if they've consumed all of their attempts for a challenge, we proactively end their session as well
            var typedState = await _gameEngine.GetChallengeState(challenge.GameEngineType, challenge.State);
            if (typedState.Challenge.Attempts >= typedState.Challenge.MaxAttempts)
            {
                await _practiceChallengeScoringListener.NotifyAttemptsExhausted(challenge, CancellationToken.None);
            }
        }
        return Mapper.Map<Challenge>(challenge);
    }

    public async Task<Challenge> Regrade(string id)
    {
        // load and regrade
        var challenge = await _challengeStore.Retrieve(id);
        double currentScore = challenge.Score;
        var state = await _gameEngine.RegradeChallenge(challenge);
        await _challengeSyncService.Sync(challenge, state, CancellationToken.None);

        // update the team score and award automatic bonuses
        await _mediator.Send(new UpdateTeamChallengeBaseScoreCommand(challenge.Id, challenge.Score));

        return Mapper.Map<Challenge>(challenge);
    }

    public async Task ArchivePlayerChallenges(Data.Player player)
    {
        // for this, we need to make sure that we're not cleaning up any challenges
        // that still belong to other members of the player's team (if they)
        // have any
        var candidateChallenges = await _challengeStore
            .List()
            .AsNoTracking()
            .Where(c => c.PlayerId == player.Id)
            .ToArrayAsync();

        var teamChallenges = await _challengeStore
            .List()
            .AsNoTracking()
            .Where(c => c.TeamId == player.TeamId && c.PlayerId != player.Id)
            .ToArrayAsync();

        var playerOnlyChallenges = candidateChallenges.Where(c => !teamChallenges.Any(tc => tc.Id == c.Id));

        await ArchiveChallenges(playerOnlyChallenges);
    }

    public async Task ArchiveTeamChallenges(string teamId)
    {
        var challenges = await _challengeStore
            .List()
            .AsNoTracking()
            .Where(c => c.TeamId == teamId)
            .ToArrayAsync();

        await ArchiveChallenges(challenges);
    }

    private async Task ArchiveChallenges(IEnumerable<Data.Challenge> challenges)
    {
        if (challenges == null || !challenges.Any())
            return;

        Logger.LogInformation($"Archiving {challenges.Count()} challenges.");
        var toArchiveIds = challenges.Select(c => c.Id).ToArray();
        var teamMemberMap = await _store
            .WithNoTracking<Data.Challenge>()
                .Include(c => c.Player)
            .Where(c => toArchiveIds.Contains(c.Id))
            .GroupBy(c => c.TeamId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(c => c.PlayerId).ToList());

        var toArchiveTasks = challenges.Select(async challenge =>
        {
            var submissions = Array.Empty<GameEngineSectionSubmission>();

            // gamespace may be deleted in TopoMojo which would cause error and prevent reset
            try
            {
                submissions = Mapper.Map<GameEngineSectionSubmission[]>(await _gameEngine.AuditChallenge(challenge));
                Logger.LogInformation($"Completing gamespace for challenge {challenge.Id}.");
                await _gameEngine.CompleteGamespace(challenge);
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
        var recordsAffected = await _challengeStore
            .DbContext
            .ArchivedChallenges
            .Where(c => toArchiveIds.Contains(c.Id))
            .ExecuteDeleteAsync();

        if (recordsAffected > 0)
            Logger.LogWarning($"While attempting to archive challenges (Ids: {string.Join(",", toArchiveIds)}) resulted in the deletion of ${recordsAffected} stale archive records.");

        _challengeStore.DbContext.ArchivedChallenges.AddRange(_mapper.Map<Data.ArchivedChallenge[]>(toArchive));
        _challengeStore.DbContext.Challenges.RemoveRange(challenges);
        await _challengeStore.DbContext.SaveChangesAsync();
    }

    public async Task<ConsoleSummary> GetConsole(ConsoleRequest model, bool observer)
    {
        var entity = await _challengeStore.Retrieve(model.SessionId);
        var challenge = Mapper.Map<Challenge>(entity);

        if (!challenge.State.Vms.Any(v => v.Name == model.Name))
            throw new ResourceNotFound<GameEngineVmState>("n/a", $"VMS for challenge {model.Name}");

        var console = await _gameEngine.GetConsole(entity, model, observer);
        return console ?? throw new InvalidConsoleAction();
    }

    public async Task<List<ObserveChallenge>> GetChallengeConsoles(string gameId)
    {
        var q = _challengeStore.DbContext.Challenges
            .Where(c => c.GameId == gameId &&
                c.HasDeployedGamespace)
            .Include(c => c.Player)
            .OrderBy(c => c.Player.Name)
            .ThenBy(c => c.Name);
        var challenges = Mapper.Map<ObserveChallenge[]>(await q.ToArrayAsync());
        var result = new List<ObserveChallenge>();
        foreach (var challenge in challenges.Where(c => c.IsActive))
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

    internal async Task<ConsoleActor> SetConsoleActor(ConsoleRequest model, string id, string name)
    {
        var entity = await _challengeStore.DbSet
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
        var entity = await _challengeStore.Load(id);
        return await _gameEngine.AuditChallenge(entity);
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
        challenge.PlayerMode = game.PlayerMode;
        challenge.WhenCreated = _now.Get();

        var state = await _gameEngine.RegisterGamespace(new GameEngineChallengeRegistration
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
        challenge.LastSyncTime = _now.Get();

        challenge.Events.Add(new ChallengeEvent
        {
            Id = _guids.GetGuid(),
            UserId = actorUserId,
            TeamId = challenge.TeamId,
            Timestamp = DateTimeOffset.UtcNow,
            Type = ChallengeEventType.Started
        });

        return challenge;
    }

    private void Transform(GameEngineGameState state)
    {
        state.Markdown = _challengeDocsService.ReplaceRelativeUris(state.Markdown);

        if (state.Challenge is not null)
            state.Challenge.Text = _challengeDocsService.ReplaceRelativeUris(state.Challenge.Text);
    }
}
