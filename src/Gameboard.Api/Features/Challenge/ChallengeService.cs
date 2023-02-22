// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TopoMojo.Api.Client;

namespace Gameboard.Api.Services
{
    public class ChallengeService : _Service
    {
        IChallengeStore Store { get; }
        GameEngineService GameEngine { get; }

        private IMemoryCache _localcache;
        private ConsoleActorMap _actorMap;
        private readonly IMapper _mapper;

        public ChallengeService(
            ILogger<ChallengeService> logger,
            IMapper mapper,
            CoreOptions options,
            IChallengeStore store,
            GameEngineService gameEngine,
            IMemoryCache localcache,
            ConsoleActorMap actorMap
        ) : base(logger, mapper, options)
        {
            Store = store;
            GameEngine = gameEngine;
            _localcache = localcache;
            _actorMap = actorMap;
            _mapper = mapper;
        }

        public async Task<Challenge> GetOrAdd(NewChallenge model, string actorId, string graderUrl)
        {
            var entity = await Store.Load(model);

            if (entity is not null)
                return Mapper.Map<Challenge>(entity);

            var player = await Store.DbContext.Players.FindAsync(model.PlayerId);

            var game = await Store.DbContext.Games
                .Include(g => g.Prerequisites)
                .Where(g => g.Id == player.GameId)
                .FirstOrDefaultAsync();

            if ((await Store.ChallengeGamespaceCount(player.TeamId)) >= game.GamespaceLimitPerSession)
                throw new GamespaceLimitReached();

            if ((await IsUnlocked(player, game, model.SpecId)).Equals(false))
                throw new ChallengeLocked();

            var lockkey = $"{player.TeamId}{model.SpecId}";
            var lockval = Guid.NewGuid();
            var locked = _localcache.GetOrCreate(lockkey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
                return lockval;
            });

            if (locked != lockval)
                throw new ChallengeStartPending();

            var spec = await Store.DbContext.ChallengeSpecs.FindAsync(model.SpecId);
            string graderKey = Guid.NewGuid().ToString("n");

            int playerCount = (game.AllowTeam)
                ? await Store.DbContext.Players.CountAsync(
                    p => p.TeamId == player.TeamId
                )
                : 1
            ;

            entity = Mapper.Map<Data.Challenge>(model);
            Mapper.Map(spec, entity);
            entity.Player = player;
            entity.TeamId = player.TeamId;
            entity.GraderKey = graderKey.ToSha256();
            Exception error = null;
            GameState state = null;

            try
            {
                state = await GameEngine.RegisterGamespace(spec, model, game, player, entity, playerCount, graderKey, graderUrl);

                Transform(state);

                Mapper.Map(state, entity);

                entity.Events.Add(new Data.ChallengeEvent
                {
                    Id = Guid.NewGuid().ToString("n"),
                    UserId = actorId,
                    TeamId = entity.TeamId,
                    Timestamp = DateTimeOffset.UtcNow,
                    Type = ChallengeEventType.Started
                });

                await Store.Create(entity);

                await Store.UpdateEtd(entity.SpecId);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                _localcache.Remove(lockkey);
            }

            if (error is Exception)
                throw error;

            return Mapper.Map<Challenge>(entity);
        }

        private async Task<bool> IsUnlocked(Data.Player player, Data.Game game, string specId)
        {
            bool result = true;

            foreach (var prereq in game.Prerequisites.Where(p => p.TargetId == specId))
            {
                var condition = await Store.DbSet.AnyAsync(c =>
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

            return await Mapper.ProjectTo<ChallengeSummary>(q).ToArrayAsync();
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

            if (entity is Data.Challenge)
                return Mapper.Map<Challenge>(entity);

            var spec = await Store.DbContext.ChallengeSpecs.FindAsync(model.SpecId);

            var cachestate = _localcache.Get<string>(spec.ExternalId);

            // Null cache state means this is a new challenge we haven't retrieved yet
            // Non-null cache state means this challenge has been retrieved before

            // No matter what, retrieve the gamespace state, because we need to handle markdown changes
            var state = await GameEngine.GetPreview(spec);

            // Transform state markdown to become more readable
            Transform(state);
            cachestate = JsonSerializer.Serialize(state);
            // Set the local cache to use this cache state for this challenge
            if (cachestate != null)
                _localcache.Set(spec.ExternalId, cachestate, new TimeSpan(0, 60, 0));

            var challenge = Mapper.Map<Data.Challenge>(spec);

            challenge.State = cachestate;

            return Mapper.Map<Challenge>(challenge);
        }

        public async Task SyncExpired()
        {
            var ts = DateTimeOffset.UtcNow;

            var challenges = await Store.DbSet
                .Where(c =>
                    c.LastSyncTime < c.Player.SessionEnd &&
                    c.Player.SessionEnd < ts
                )
                .ToArrayAsync()
            ;

            foreach (var challenge in challenges)
                _actorMap.RemoveTeam(challenge.TeamId);

            var tasks = challenges.Select(
                c => Sync(c)
            );

            await Task.WhenAll(tasks);
        }

        private async Task<Data.Challenge> Sync(Data.Challenge entity, Task<GameState> task = null)
        {
            if (task is null)
                task = GameEngine.LoadGamespace(entity);

            try
            {
                var state = await task;

                Mapper.Map(state, entity);
            }
            catch (Exception ex)
            {
                entity.LastSyncTime = DateTimeOffset.UtcNow;
                Logger.LogError(ex, "Sync error on {0} {1}", entity.Id, entity.Name);
            }

            await Store.Update(entity);

            return entity;
        }

        private async Task<Data.Challenge> Sync(string id, Task<GameState> task = null)
        {
            var entity = await Store.Retrieve(id);

            return await Sync(entity, task);
        }

        public async Task<Challenge> StartGamespace(string id, string actorId)
        {
            var entity = await Store.Retrieve(id);

            var game = await Store.DbContext.Games.FindAsync(entity.GameId);

            if ((await Store.ChallengeGamespaceCount(entity.TeamId)) >= game.GamespaceLimitPerSession)
                throw new GamespaceLimitReached();

            entity.Events.Add(new Data.ChallengeEvent
            {
                Id = Guid.NewGuid().ToString("n"),
                UserId = actorId,
                TeamId = entity.TeamId,
                Timestamp = DateTimeOffset.UtcNow,
                Type = ChallengeEventType.GamespaceOn
            });

            await Sync(
                entity,
                GameEngine.StartGamespace(entity)
            );

            return Mapper.Map<Challenge>(entity);
        }

        public async Task<Challenge> StopGamespace(string id, string actorId)
        {
            var entity = await Store.Retrieve(id);

            entity.Events.Add(new Data.ChallengeEvent
            {
                Id = Guid.NewGuid().ToString("n"),
                UserId = actorId,
                TeamId = entity.TeamId,
                Timestamp = DateTimeOffset.UtcNow,
                Type = ChallengeEventType.GamespaceOff
            });

            await Sync(
                entity,
                GameEngine.StopGamespace(entity)
            );

            return Mapper.Map<Challenge>(entity);
        }

        public async Task<Challenge> Grade(SectionSubmission model, string actorId)
        {
            var entity = await Store.Retrieve(model.Id);

            // TODO: don't log auto-grader events
            // if (model.Id != actorId)
            entity.Events.Add(new Data.ChallengeEvent
            {
                Id = Guid.NewGuid().ToString("n"),
                UserId = actorId,
                TeamId = entity.TeamId,
                Timestamp = DateTimeOffset.UtcNow,
                Type = ChallengeEventType.Submission
            });

            double currentScore = entity.Score;

            Task<GameState> gradingTask = GameEngine.GradeChallenge(entity, model);

            var result = await Sync(
                entity,
                gradingTask
            );

            if (result.Score > currentScore)
                await Store.UpdateTeam(entity.TeamId);

            return Mapper.Map<Challenge>(entity);
        }

        public async Task<Challenge> Regrade(string id)
        {
            var entity = await Store.Retrieve(id);

            double currentScore = entity.Score;

            var result = await Sync(
                entity,
                GameEngine.RegradeChallenge(entity)
            );

            if (result.Score > currentScore)
                await Store.UpdateTeam(entity.TeamId);

            return Mapper.Map<Challenge>(entity);
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
            var challenges = await Store
                .List()
                .AsNoTracking()
                .Where(c => c.TeamId == teamId)
                .ToArrayAsync();

            await ArchiveChallenges(challenges);
        }

        private async Task ArchiveChallenges(IEnumerable<Data.Challenge> challenges)
        {
            if (challenges.Count() > 0)
            {
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
                    var submissions = new SectionSubmission[] { };

                    // gamespace may be deleted in TopoMojo which would cause error and prevent reset
                    try
                    {
                        submissions = await GameEngine.AuditChallenge(challenge);
                        if (challenge.HasDeployedGamespace)
                            await GameEngine.CompleteGamespace(challenge);
                    }
                    catch
                    {
                        // no-op - leave as empty array
                    }

                    var mapped = _mapper.Map<Api.ArchivedChallenge>(challenge);
                    mapped.Submissions = submissions;
                    mapped.TeamMembers = teamMemberMap[challenge.TeamId].ToArray();

                    return mapped;
                }).ToArray();

                var toArchive = await Task.WhenAll(toArchiveTasks);

                Store.DbContext.ArchivedChallenges.AddRange(_mapper.Map<Data.ArchivedChallenge[]>(toArchive));
                await Store.DbContext.SaveChangesAsync();
            }
        }

        public async Task<ConsoleSummary> GetConsole(ConsoleRequest model, bool observer)
        {
            var entity = await Store.Retrieve(model.SessionId);
            var challenge = Mapper.Map<Challenge>(entity);

            if (!challenge.State.Vms.Any(v => v.Name == model.Name))
                throw new ResourceNotFound<VmState>("n/a", $"VMS for challenge {model.Name}");

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

        internal async Task<SectionSubmission[]> Audit(string id)
        {
            var entity = await Store.Load(id);
            return await GameEngine.AuditChallenge(entity);
        }

        private void Transform(GameState state)
        {
            state.Markdown = state.Markdown.Replace("](/docs", $"]({Options.ChallengeDocUrl}docs");

            if (state.Challenge is not null)
                state.Challenge.Text = state.Challenge.Text.Replace("](/docs", $"]({Options.ChallengeDocUrl}docs");
        }
    }

}
