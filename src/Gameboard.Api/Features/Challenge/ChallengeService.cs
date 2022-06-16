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
    public class ChallengeService : _Service, IApiKeyAuthenticationService
    {
        IChallengeStore Store { get; }
        ITopoMojoApiClient Mojo { get; }

        private IMemoryCache _localcache;
        private ConsoleActorMap _actorMap;

        public ChallengeService(
            ILogger<ChallengeService> logger,
            IMapper mapper,
            CoreOptions options,
            IChallengeStore store,
            ITopoMojoApiClient mojo,
            IMemoryCache localcache,
            ConsoleActorMap actorMap
        ) : base(logger, mapper, options)
        {
            Store = store;
            Mojo = mojo;
            _localcache = localcache;
            _actorMap = actorMap;
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
                .FirstOrDefaultAsync()
            ;

            if ((await Store.ChallengeGamespaceCount(player.TeamId)) >= game.GamespaceLimitPerSession)
                throw new GamespaceLimitReached();

            if ((await IsUnlocked(player, game, model.SpecId)).Equals(false))
                throw new ChallengeLocked();

            var lockkey = $"{player.TeamId}{model.SpecId}";
            var lockval = Guid.NewGuid();
            var locked = _localcache.GetOrCreate(lockkey, entry => {
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

            try {
                var state = await Mojo.RegisterGamespaceAsync(new GamespaceRegistration
                {
                    Players = new RegistrationPlayer[] {
                        new RegistrationPlayer {
                            SubjectId = player.TeamId,
                            SubjectName = player.Name
                        }
                    },
                    ResourceId = entity.ExternalId,
                    Variant = model.Variant,
                    Points = spec.Points,
                    MaxAttempts = game.MaxAttempts,
                    StartGamespace = true,
                    ExpirationTime = entity.Player.SessionEnd,
                    GraderKey = graderKey,
                    GraderUrl = graderUrl,
                    PlayerCount = playerCount
                });

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
                throw ex;
            }
            finally
            {
               _localcache.Remove(lockkey);
            }

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

        // public async Task Update(ChangedChallenge model)
        // {
        //     var entity = await Store.Retrieve(model.Id);

        //     Mapper.Map(model, entity);

        //     await Store.Update(entity);
        // }

        public async Task Delete(string id)
        {
            await Store.Delete(id);
            await Mojo.DeleteGamespaceAsync(id);
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

            // todo other filtering

            q = q.Include(c => c.Player).Include(c => c.Game);

            DateTimeOffset recent = DateTimeOffset.Now.AddDays(-1);

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

            //check preview cache, else mojo
            var cachestate = _localcache.Get<string>(spec.ExternalId);

            if (cachestate == null)
            {
                var state = await Mojo.PreviewGamespaceAsync(spec.ExternalId);

                Transform(state);

                cachestate = JsonSerializer.Serialize(state);

                if (cachestate != null)
                    _localcache.Set(spec.ExternalId, cachestate, new TimeSpan(0, 60, 0));

            }

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
                task = Mojo.LoadGamespaceAsync(entity.Id);

            try
            {
                var state = await task;

                Mapper.Map(state, entity);
            }
            catch (Exception ex)
            {
                entity.LastSyncTime = DateTimeOffset.Now;
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
                Mojo.StartGamespaceAsync(id)
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
                Mojo.StopGamespaceAsync(id)
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

            var result = await Sync(
                entity,
                Mojo.GradeChallengeAsync(model)
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
                Mojo.RegradeChallengeAsync(id)
            );

            if (result.Score > currentScore)
                await Store.UpdateTeam(entity.TeamId);

            return Mapper.Map<Challenge>(entity);
        }

        public async Task<ConsoleSummary> GetConsole(ConsoleRequest model, bool observer)
        {
            var challenge = Mapper.Map<Challenge>(
                await Store.Retrieve(model.SessionId)
            );

            if (!challenge.State.Vms.Any(v => v.Name == model.Name))
                throw new ResourceNotFound();

            switch (model.Action)
            {
                case ConsoleAction.Ticket:

                    return Mapper.Map<ConsoleSummary>(
                        await Mojo.GetVmTicketAsync(model.Id)
                    );

                case ConsoleAction.Reset:

                    var vm = await Mojo.ChangeVmAsync(
                        new VmOperation
                        {
                            Id = model.Id,
                            Type = VmOperationType.Reset
                        }
                    );

                    return new ConsoleSummary
                    {
                        Id = vm.Id,
                        Name = vm.Name,
                        SessionId = model.SessionId,
                        IsRunning = vm.State == VmPowerState.Running,
                        IsObserver = observer
                    };

            }

            throw new InvalidConsoleAction();
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

        public async Task<string> ResolveApiKey(string key)
        {
            if (key.IsEmpty())
                return null;

            var entity = await Store.ResolveApiKey(key.ToSha256());

            return entity?.Id;
        }

        internal async Task<ConsoleActor> SetConsoleActor(ConsoleRequest model, string id, string name)
        {
            var entity = await Store.DbSet
                .Include(c => c.Player)
                .FirstOrDefaultAsync(c => c.Id == model.SessionId)
            ;

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
            return (await Mojo.AuditChallengeAsync(id)).ToArray();
        }

        private void Transform(GameState state)
        {
            state.Markdown = state.Markdown.Replace("](/docs", $"]({Options.ChallengeDocUrl}docs");

            if (state.Challenge is not null)
                state.Challenge.Text = state.Challenge.Text.Replace("](/docs", $"]({Options.ChallengeDocUrl}docs");
        }
    }

}
