// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services;

public partial class ChallengeService
(
    IActingUserService actingUserService,
    ConsoleActorMap actorMap,
    CoreOptions coreOptions,
    IChallengeStore challengeStore,
    IChallengeDocsService challengeDocsService,
    IChallengeSubmissionsService challengeSubmissionsService,
    IChallengeSyncService challengeSyncService,
    IGameEngineService gameEngine,
    IGuidService guids,
    IJsonService jsonService,
    ILogger<ChallengeService> logger,
    IMapper mapper,
    IMediator mediator,
    INowService now,
    IPracticeService practiceService,
    IUserRolePermissionsService permissionsService,
    IStore store,
    ITeamService teamService
) : _Service(logger, mapper, coreOptions)
{
    private readonly IActingUserService _actingUserService = actingUserService;
    private readonly ConsoleActorMap _actorMap = actorMap;
    private readonly IChallengeStore _challengeStore = challengeStore;
    private readonly IGameEngineService _gameEngine = gameEngine;
    private readonly IGuidService _guids = guids;
    private readonly IJsonService _jsonService = jsonService;
    private readonly static ConcurrentDictionary<string, ChallengeLaunchCacheEntry> _launchCache = new();
    private readonly IMapper _mapper = mapper;
    private readonly IMediator _mediator = mediator;
    private readonly INowService _now = now;
    private readonly IPracticeService _practiceService = practiceService;
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    private readonly IChallengeDocsService _challengeDocsService = challengeDocsService;
    private readonly IChallengeSubmissionsService _challengeSubmissionsService = challengeSubmissionsService;
    private readonly IChallengeSyncService _challengeSyncService = challengeSyncService;
    private readonly IStore _store = store;
    private readonly ITeamService _teamService = teamService;

    public async Task<Challenge> GetOrCreate(NewChallenge model, string actorId, string graderUrl)
    {
        var entity = await _challengeStore.Load(model);

        if (entity is not null)
            return Mapper.Map<Challenge>(entity);

        return await Create(model, actorId, graderUrl, CancellationToken.None);
    }

    public int GetDeployingChallengeCount(string teamId)
    {
        if (!_launchCache.TryGetValue(teamId, out var entry))
        {
            return 0;
        }

        return entry.Specs.Count;
    }

    public IEnumerable<string> GetTags(Data.ChallengeSpec spec)
    {
        if (spec.Tags.IsEmpty())
            return [];

        return CommonRegexes
            .WhitespaceGreedy
            .Split(spec.Tags)
            .Select(m => m.Trim().ToLower())
            .Where(m => m.IsNotEmpty())
            .ToArray();
    }

    public async Task<Challenge> Create(NewChallenge model, string actorId, string graderUrl, CancellationToken cancellationToken)
    {
        var now = _now.Get();
        var player = await _store
            .WithNoTracking<Data.Player>()
            .Include(p => p.Game)
            .SingleAsync(p => p.Id == model.PlayerId, cancellationToken);

        if (!await IsUnlocked(player, player.Game, model.SpecId))
        {
            throw new ChallengeLocked();
        }

        // if we're outside the execution window, we need to be sure the acting person is an admin
        if (player.Game.IsCompetitionMode)
        {
            // check gamespace limits for competitive games only
            var teamActiveChallenges = await _teamService.GetChallengesWithActiveGamespace(player.TeamId, player.GameId, cancellationToken);
            var activePlusPendingChallengeCount = teamActiveChallenges.Count() + GetDeployingChallengeCount(player.TeamId);
            if (activePlusPendingChallengeCount >= player.Game.GamespaceLimitPerSession)
            {
                throw new GamespaceLimitReached(player.GameId, player.TeamId);
            }

            if (now > player.Game.GameEnd)
            {
                // Would ideally do this using the acting user service, but background deployment (caused by sync start)
                // may not play well with that as of now.
                // var actingUser = _actingUserService.Get();
                var actingUser = await _store.WithNoTracking<Data.User>().SingleOrDefaultAsync(u => u.Id == actorId, cancellationToken);

                if (!await _permissionsService.Can(PermissionKey.Play_IgnoreExecutionWindow))
                    throw new CantStartBecauseGameExecutionPeriodIsOver(model.SpecId, model.PlayerId, player.Game.GameEnd, now);
            }
        }

        _launchCache.EnsureKey(player.TeamId, new ChallengeLaunchCacheEntry
        {
            TeamId = player.TeamId,
            Specs = []
        });

        _launchCache.TryGetValue(player.TeamId, out var entry);

        if (entry.Specs.Any(s => s.SpecId == model.SpecId))
        {
            throw new ChallengeStartPending();
        }
        else
        {
            entry.Specs.Add(new ChallengeLaunchCacheEntrySpec { GameId = player.GameId, SpecId = model.SpecId });
        }

        var spec = await _store
            .WithNoTracking<Data.ChallengeSpec>()
            .SingleAsync(s => s.Id == model.SpecId, cancellationToken);

        var playerCount = 1;
        if (player.Game.AllowTeam)
        {
            playerCount = await _store
                .WithNoTracking<Data.Player>()
                .CountAsync(p => p.TeamId == player.TeamId, cancellationToken);
        }

        try
        {
            var challenge = await BuildAndRegisterChallenge(model, spec, player.Game, player, actorId, graderUrl, playerCount, model.Variant);

            await _store.Create(challenge, cancellationToken);
            await _challengeStore.UpdateEtd(challenge.SpecId);

            return Mapper.Map<Challenge>(challenge);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(message: "Challenge registration failure: {exName} -- {exMessage}", ex.GetType().Name, ex.Message);
            throw;
        }
        finally
        {
            entry.Specs = entry.Specs.Where(s => s.SpecId != model.SpecId).ToList();
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
        var result = Mapper.Map<Challenge>(await _challengeStore.Load(id));

        return result;
    }

    public async Task Delete(string id)
    {
        await _challengeStore.Delete(id);
        var entity = await _challengeStore.Load(id);
        await _gameEngine.DeleteGamespace(entity);
    }

    public async Task<bool> UserIsPlayingChallenge(string challengeId, string userId)
    {
        var challengeTeamId = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.Id == challengeId)
            .Select(c => c.TeamId)
            .Distinct()
            .SingleOrDefaultAsync();

        var userTeamIds = await _teamService.GetUserTeamIds(userId);
        return userTeamIds.Any(tId => tId == challengeTeamId);
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
        var teamPlayerMap = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => teamIds.Contains(p.TeamId))
            .GroupBy(p => p.TeamId)
            .ToDictionaryAsync(g => g.Key, g => g);

        foreach (var summary in summaries)
        {
            if (teamPlayerMap.TryGetValue(summary.TeamId, out IGrouping<string, Data.Player> value))
            {
                var teamPlayers = value;
                summary.Players = teamPlayers.Select(p => _mapper.Map<ChallengePlayer>(p));
            }
            else
            {
                summary.Players = [];
            }
        }

        return summaries;
    }

    public async Task<ChallengeOverview[]> ListByUser(string uid)
    {
        var q = _challengeStore.List(null);

        var userTeams = await _store
            .WithNoTracking<Data.Player>()
                .Where(p => p.UserId == uid && p.TeamId != null && p.TeamId != "")
                .Select(p => p.TeamId)
                .ToListAsync();

        q = q.Where(t => userTeams.Any(i => i == t.TeamId));

        var recent = DateTimeOffset.UtcNow.AddDays(-1);
        var practiceChallengesCutoff = _now.Get().AddDays(-7);
        q = q.Include(c => c.Player).Include(c => c.Game);
        // band-aid for #296
        q = q.Where
        (
            c =>
                c.EndTime > recent ||
                c.Game.GameEnd > recent ||
                (c.PlayerMode == PlayerMode.Practice && c.StartTime >= practiceChallengesCutoff)
        );
        q = q.OrderByDescending(p => p.StartTime);

        return await Mapper.ProjectTo<ChallengeOverview>(q).ToArrayAsync();
    }

    public async Task<ArchivedChallenge[]> ListArchived(SearchFilter model)
    {
        var q = _store.WithNoTracking<Data.ArchivedChallenge>();

        if (model.Term.NotEmpty())
        {
            var term = model.Term.ToLower();
            q = q.Where
            (
                c =>
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

        var spec = await _store.SingleAsync<Data.ChallengeSpec>(model.SpecId, CancellationToken.None);
        var challenge = Mapper.Map<Data.Challenge>(spec);

        var result = Mapper.Map<Challenge>(challenge);
        GameEngineGameState state = new() { Markdown = spec.Text };
        state = TransformStateRelativeUrls(state);
        result.State = state;
        return result;
    }

    public async Task<Challenge> StartGamespace(string id, string actorId, CancellationToken cancellationToken)
    {
        var challenge = await _challengeStore.Retrieve(id);
        var game = await _store.SingleAsync<Data.Game>(challenge.GameId, cancellationToken);

        if (await _teamService.IsAtGamespaceLimit(challenge.TeamId, game, cancellationToken))
            throw new GamespaceLimitReached(game.Id, challenge.TeamId);

        var state = await _gameEngine.StartGamespace(new GameEngineGamespaceStartRequest { ChallengeId = challenge.Id, GameEngineType = challenge.GameEngineType });
        state = TransformStateRelativeUrls(state);
        await _challengeSyncService.Sync(challenge, state, actorId, cancellationToken);

        return Mapper.Map<Challenge>(challenge);
    }

    public async Task<Challenge> StopGamespace(string id, string actorId)
    {
        var challenge = await _challengeStore.Retrieve(id);
        var state = await _gameEngine.StopGamespace(challenge);
        state = TransformStateRelativeUrls(state);

        await _challengeSyncService.Sync(challenge, state, actorId, CancellationToken.None);
        return Mapper.Map<Challenge>(challenge);
    }

    public async Task<Challenge> Grade(GameEngineSectionSubmission model, User actor, CancellationToken cancellationToken)
    {
        var now = _now.Get();
        var challenge = await _store
            .WithNoTracking<Data.Challenge>()
            .SingleAsync(c => c.Id == model.Id, cancellationToken);

        // have to retrieve game end separately due to a bug with Store (tracked entity issue)
        var gameProperties = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == challenge.GameId)
            .Select(g => new { g.PlayerMode, g.GameEnd })
            .SingleAsync(cancellationToken);

        // ensure that the game hasn't ended - if it has, we have to bounce this one
        var canPlayOutsideExecutionWindow = await _permissionsService.Can(PermissionKey.Play_IgnoreExecutionWindow);
        if (!canPlayOutsideExecutionWindow && gameProperties.PlayerMode == PlayerMode.Competition && now > gameProperties.GameEnd)
        {
            await _store.Create(new ChallengeEvent
            {
                Id = _guids.Generate(),
                ChallengeId = challenge.Id,
                UserId = actor?.Id ?? null,
                TeamId = challenge.TeamId,
                Timestamp = now,
                Type = ChallengeEventType.SubmissionRejectedGameEnded
            });

            throw new CantGradeBecauseGameExecutionPeriodIsOver(challenge.Id, gameProperties.GameEnd, now);
        }

        // determine how many attempts have been made prior to this one
        var priorAttemptCount = 0;

        if (challenge.State is not null)
        {
            var preGradeState = _jsonService.Deserialize<GameEngineGameState>(challenge.State);
            priorAttemptCount = preGradeState.Challenge.Attempts;
        }

        // log the appropriate event
        await _store.Create(new ChallengeEvent
        {
            Id = _guids.Generate(),
            ChallengeId = challenge.Id,
            UserId = actor?.Id ?? null,
            TeamId = challenge.TeamId,
            Timestamp = now,
            Type = ChallengeEventType.Submission
        });

        var postGradingState = default(GameEngineGameState);

        try
        {
            postGradingState = await _gameEngine.GradeChallenge(challenge, model);
        }
        catch (SubmissionIsForExpiredGamespace)
        {
            Logger.LogInformation($"Rejected a submission for challenge {challenge.Id}: the gamespace is expired.");

            var challengeEvent = new ChallengeEvent
            {
                Id = _guids.Generate(),
                ChallengeId = challenge.Id,
                UserId = actor?.Id ?? null,
                TeamId = challenge.TeamId,
                Timestamp = now,
                Type = ChallengeEventType.SubmissionRejectedGamespaceExpired
            };

            // save and add the event to the entity for the return value
            await _store.Create(challengeEvent);

            throw;
        }

        if (postGradingState is null)
            throw new InvalidOperationException("The post-grading state of the challenge was null.");

        await _challengeSyncService.Sync(challenge, postGradingState, actor.Id, CancellationToken.None);

        // update the team score and award automatic bonuses
        var updatedScore = await _mediator.Send(new UpdateTeamChallengeBaseScoreCommand(challenge.Id, challenge.Score), cancellationToken);

        // update the challenge object with the score (note that we omit bonuses here because we want the 
        // score in the players table only to count base completion score for now
        challenge.Score = updatedScore.Score.CompletionScore;

        // The game engine (Topo, in most cases) may optionally not count this as an attempt if the answers are identical 
        // to a previous attempt, which means we need to be sure this consumed an attempt
        // before counting it as a new submission for logging purposes.
        if (postGradingState.Challenge.Attempts > priorAttemptCount)
        {
            // record the submission
            await _challengeSubmissionsService.LogSubmission
            (
                challenge.Id,
                (int)updatedScore.Score.TotalScore,
                model.SectionIndex,
                model.Questions.Select(q => q.Answer),
                cancellationToken
            );
        }

        // in practice, we sometimes proactively end the session
        if (challenge.PlayerMode == PlayerMode.Practice)
        {
            if (challenge.Score >= challenge.Points)
            {
                // if they complete the challenge
                await _teamService.EndSession(challenge.TeamId, actor, CancellationToken.None);
            }
            else
            {
                var settings = await _practiceService.GetSettings(cancellationToken);

                // or if the practice area has an attempt limit and it's been exceeded
                if (settings.AttemptLimit is not null && postGradingState.Challenge.Attempts > settings.AttemptLimit)
                {
                    await _teamService.EndSession(challenge.TeamId, actor, cancellationToken);
                }
            }
        }

        return Mapper.Map<Challenge>(challenge);
    }

    public async Task<Challenge> Regrade(string id)
    {
        // load and regrade
        var challenge = await _challengeStore.Retrieve(id);
        // preserve the score prior to regrade
        var currentScore = challenge.Score;
        // who's regrading?
        var actingUserId = _actingUserService.Get()?.Id;

        var state = await _gameEngine.RegradeChallenge(challenge);
        await _challengeSyncService.Sync(challenge, state, actingUserId, CancellationToken.None);

        // log an event for successful regrading
        await _store.Create(new ChallengeEvent
        {
            ChallengeId = id,
            TeamId = challenge.TeamId,
            Timestamp = _now.Get(),
            Type = ChallengeEventType.Regraded,
            UserId = actingUserId
        });

        // update the team score and award automatic bonuses
        if (state.Challenge.Score != currentScore)
            await _mediator.Send(new UpdateTeamChallengeBaseScoreCommand(challenge.Id, challenge.Score));
        else
            Logger.LogWarning($"Regrade of challenge {id} didn't result in a score change.");

        return Mapper.Map<Challenge>(challenge);
    }

    public async Task ArchivePlayerChallenges(Data.Player player)
    {
        // for this, we need to make sure that we're not cleaning up any challenges
        // that still belong to other members of the player's team (if they)
        // have any
        var candidateChallenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.PlayerId == player.Id)
            .ToArrayAsync();

        var teamChallengeIds = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.TeamId == player.TeamId && c.PlayerId != player.Id)
            .Select(c => c.Id)
            .ToArrayAsync();

        var playerOnlyChallenges = candidateChallenges
            .Where(c => !teamChallengeIds.Any(tcId => tcId == c.Id));

        await ArchiveChallenges(playerOnlyChallenges);
    }

    public async Task ArchiveTeamChallenges(string teamId)
    {
        var challenges = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.TeamId == teamId)
            .ToArrayAsync();

        await ArchiveChallenges(challenges);
    }

    private async Task ArchiveChallenges(IEnumerable<Data.Challenge> challenges)
    {
        if (challenges == null || !challenges.Any())
            return;

        Logger.LogInformation("Archiving {challengeCount} challenges.", challenges.Count());
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
                Logger.LogWarning("Exception thrown during attempted cleanup of gamespace (type: {exType}, message: {message})", ex.GetType().Name, ex.Message);
            }

            var mappedChallenge = _mapper.Map<ArchivedChallenge>(challenge);
            mappedChallenge.Submissions = submissions;
            mappedChallenge.TeamMembers = teamMemberMap.TryGetValue(challenge.TeamId, out List<string> value) ? [.. value] : [];

            return mappedChallenge;
        }).ToArray();

        var toArchive = await Task.WhenAll(toArchiveTasks);

        // handle challenges that have been archived before
        var recordsAffected = await _store
            .WithNoTracking<Data.ArchivedChallenge>()
            .Where(c => toArchiveIds.Contains(c.Id))
            .ExecuteDeleteAsync();

        if (recordsAffected > 0)
            Logger.LogWarning($"While attempting to archive challenges (Ids: {string.Join(",", toArchiveIds)}) resulted in the deletion of ${recordsAffected} stale archive records.");

        await _store.DoTransaction(async dbContext =>
        {
            // NOTE: see this function's comments to understand why it's here (and why it should someday go away)
            dbContext.DetachUnchanged();
            await dbContext.ArchivedChallenges.AddRangeAsync(_mapper.Map<Data.ArchivedChallenge[]>(toArchive));
            dbContext.Challenges.RemoveRange(challenges);
            await dbContext.SaveChangesAsync();
        }, CancellationToken.None);
    }

    public async Task<ConsoleSummary> GetConsole(ConsoleRequest model, bool observer)
    {
        var entity = await _challengeStore.Retrieve(model.SessionId);
        var challenge = Mapper.Map<Challenge>(entity);

        if (!challenge.State.Vms.Any(v => v.Name == model.Name))
        {
            var vmNames = string.Join(", ", challenge.State.Vms.Select(vm => vm.Name));
            throw new ResourceNotFound<GameEngineVmState>("n/a", $"VMS for challenge {model.Name} - searching for {model.Name}, found these names: {vmNames}");
        }

        var console = await _gameEngine.GetConsole(entity, model, observer);
        return console ?? throw new InvalidConsoleAction();
    }

    public async Task<List<ObserveChallenge>> GetChallengeConsoles(string gameId)
    {
        // retrieve challenges to list
        var q = _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.GameId == gameId && c.HasDeployedGamespace)
            .Include(c => c.Player)
            .OrderBy(c => c.Player.Name)
            .ThenBy(c => c.Name);
        var challenges = Mapper.Map<ObserveChallenge[]>(await q.ToArrayAsync());

        // resolve the name of the captain.
        // (we used to depend on the name of all players being that of the captain, but
        // we don't anymore because that was super unstable and weird)
        var teamIds = challenges.Select(c => c.TeamId).ToArray();
        var captains = await _teamService.ResolveCaptains(teamIds, CancellationToken.None);

        var result = new List<ObserveChallenge>();
        foreach (var challenge in challenges.Where(c => c.IsActive))
        {
            // attempt to grab the captain's name if we were able to resolve it from the
            // teamservice
            var captain = captains.ContainsKey(challenge.TeamId) ? captains[challenge.TeamId] : null;
            challenge.TeamName = captain?.ApprovedName ?? challenge.PlayerName;

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

    public async Task<ChallengeIdUserIdMap> GetChallengeUserMaps(IQueryable<Data.Challenge> query, CancellationToken cancellationToken)
    {
        var teamChallengeIds = await query
            .Select(c => new { c.Id, c.TeamId })
            .GroupBy(c => c.TeamId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.Select(c => c.Id).Distinct().ToArray(), cancellationToken);

        var teamIds = teamChallengeIds.Keys;

        var userTeamIds = await _store
            .WithNoTracking<Data.Player>()
            .Where(p => p.UserId != null && p.UserId != string.Empty)
            .Where(p => teamIds.Contains(p.TeamId))
            .Select(p => new { p.UserId, p.TeamId })
            .GroupBy(p => p.UserId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.Select(thing => thing.TeamId).Distinct(), cancellationToken);

        var userIdChallengeIds = userTeamIds
            .ToDictionary(gr => gr.Key, gr => gr.Value
            .SelectMany(tId => teamChallengeIds[tId]));

        var challengeIdUserIds = new Dictionary<string, IEnumerable<string>>();
        foreach (var kv in userIdChallengeIds)
            foreach (var cId in kv.Value)
                if (challengeIdUserIds.TryGetValue(cId, out IEnumerable<string> userIds))
                    _ = userIds.Append(kv.Key);
                else
                    challengeIdUserIds[cId] = [kv.Key];

        return new ChallengeIdUserIdMap
        {
            ChallengeIdUserIds = challengeIdUserIds,
            UserIdChallengeIds = userIdChallengeIds
        };
    }

    public GameEngineGameState TransformStateRelativeUrls(GameEngineGameState state)
    {
        state.Markdown = _challengeDocsService.ReplaceRelativeUris(state.Markdown);

        if (state.Challenge is not null)
            state.Challenge.Text = _challengeDocsService.ReplaceRelativeUris(state.Challenge.Text);

        return state;
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
        var graderKey = _guids.Generate();
        var challenge = Mapper.Map<Data.Challenge>(newChallenge);
        Mapper.Map(spec, challenge);
        challenge.PlayerId = player.Id;
        challenge.TeamId = player.TeamId;
        challenge.GraderKey = graderKey.ToSha256();
        challenge.PlayerMode = game.PlayerMode;
        challenge.WhenCreated = _now.Get();

        var attemptLimit = game.MaxAttempts;
        if (game.PlayerMode == PlayerMode.Practice)
        {
            var settings = await _practiceService.GetSettings(CancellationToken.None);
            attemptLimit = settings.AttemptLimit ?? 0;
        }

        var state = await _gameEngine.RegisterGamespace(new GameEngineChallengeRegistration
        {
            AttemptLimit = attemptLimit,
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
        state = TransformStateRelativeUrls(state);

        // manually map here - we need the player object and other references to stay the same for
        // db add
        challenge.Id = state.Id;
        challenge.ExternalId = spec.ExternalId;
        challenge.HasDeployedGamespace = state.IsActive;
        challenge.State = _jsonService.Serialize(state);
        challenge.StartTime = state.StartTime;
        challenge.LastSyncTime = _now.Get();

        // if we haven't already resolved the endtime
        if (challenge.EndTime.IsEmpty())
        {
            // prefer the state's end time
            if (state.EndTime.IsNotEmpty())
            {
                challenge.EndTime = state.EndTime;
            }
            // but fall back on the expiration time
            else if (state.ExpirationTime.IsNotEmpty())
            {
                challenge.EndTime = state.ExpirationTime;
            }
        }

        challenge.Events.Add(new ChallengeEvent
        {
            Id = _guids.Generate(),
            UserId = actorUserId,
            TeamId = challenge.TeamId,
            Timestamp = _now.Get(),
            Type = ChallengeEventType.Started
        });

        return challenge;
    }
}
