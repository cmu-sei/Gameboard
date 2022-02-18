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
    public class FeedbackService : _Service, IApiKeyAuthenticationService
    {
        IChallengeStore ChallengeStore { get; }
        IFeedbackStore Store { get; }
        ITopoMojoApiClient Mojo { get; }

        private IMemoryCache _localcache;
        private ConsoleActorMap _actorMap;

        public FeedbackService(
            ILogger<FeedbackService> logger,
            IMapper mapper,
            CoreOptions options,
            IChallengeStore challenegeStore,
            IFeedbackStore store,
            ITopoMojoApiClient mojo,
            IMemoryCache localcache,
            ConsoleActorMap actorMap
        ) : base(logger, mapper, options)
        {
            ChallengeStore = challenegeStore;
            Store = store;
            Mojo = mojo;
            _localcache = localcache;
            _actorMap = actorMap;
        }
 
        public async Task<Feedback> Retrieve(FeedbackSearchParams model, string actorId)
        {
            var entity = await Store.List().FirstOrDefaultAsync(s =>
                s.ChallengeSpecId == model.ChallengeSpecId &&
                s.GameId == model.GameId &&
                s.UserId == actorId
            );
            return Mapper.Map<Feedback>(entity);
        }

       
        public async Task<Feedback> Submit(FeedbackSubmission model, string actorId)
        {
            // var challengeEntity = await ChallengeStore.Retrieve(model.ChallengeId);
            // var entity = await Store.Retrieve(model.Id);


            var entity = await Store.List()
                .FirstOrDefaultAsync(s =>
                    s.ChallengeSpecId == model.ChallengeSpecId &&
                    s.ChallengeId == model.ChallengeId &&
                    s.UserId == actorId &&
                    s.GameId == model.GameId
                );

            if (model.Submit) { // Only validate on submit as an optimization
                var valid = await ValidateFeedback(model.Questions, model.GameId, model.ChallengeId);
                if (!valid)
                    throw new InvalideFeedbackFormat();
            }

            if (entity is Data.Feedback)
            {
                if (entity.Submitted) {
                    return Mapper.Map<Feedback>(entity);
                }
                Mapper.Map(model, entity);
                entity.UserId = actorId;
                entity.Timestamp = DateTimeOffset.UtcNow;
                await Store.Update(entity);
            }
            else // create new 
            {
                var player = await Store.DbContext.Players.FirstOrDefaultAsync(s =>
                    s.UserId == actorId &&
                    s.GameId == model.GameId
                );
                if (player == null) {
                    throw new ResourceNotFound();
                }

                entity = Mapper.Map<Data.Feedback>(model);
                entity.UserId = actorId;
                entity.PlayerId = player.Id;
                entity.Id = Guid.NewGuid().ToString("n");
                entity.Timestamp = DateTimeOffset.UtcNow;
                await Store.Create(entity);
            }

            return Mapper.Map<Feedback>(entity);
        }

        public async Task<FeedbackReportDetails[]> List(FeedbackSearchParams model)
        {
            var q = Store.List(model.Term);
            
            if (model.GameId.HasValue())
                q = q.Where(u => u.GameId == model.GameId);
            
            if (model.Type == "game")
                q = q.Where(u => u.ChallengeSpecId == null);
            else if (model.Type == "challenge")
                q = q.Where(u => u.ChallengeSpecId != null);

            if (model.ChallengeSpecId.HasValue())
                q = q.Where(u => u.ChallengeSpecId == model.ChallengeSpecId);

            q = q.Include(p => p.Player).Include(p => p.ChallengeSpec);

            return await Mapper.ProjectTo<FeedbackReportDetails>(q).ToArrayAsync();
        }

        public async Task<Feedback[]> ListByChallengeSpec(string id)
        {
            var q = Store.List();

            q = q.Where(u => u.ChallengeSpecId == id);

            return await Mapper.ProjectTo<Feedback>(q).ToArrayAsync();
        }

        public async Task<string> ResolveApiKey(string key)
        {
            if (key.IsEmpty())
                return null;

            var entity = await Store.ResolveApiKey(key.ToSha256());

            return entity?.Id;
        }

        private async Task<bool> ValidateFeedback(FeedbackQuestion[] feedback, string gameId, string challengeId) {
            var game = Mapper.Map<Game>(await Store.DbContext.Games.FindAsync(gameId));
            QuestionTemplate[] feedbackTemplate;
            if (challengeId.IsEmpty())
                feedbackTemplate = game.FeedbackTemplate.Board;
            else 
                feedbackTemplate = game.FeedbackTemplate.Challenge;

            if (feedbackTemplate.Length != feedback.Length)
                return false;
            // naive approach, only run on 'submit' and not 'autosave'?
            foreach (var q in feedback) 
            {
                var template = Array.Find<QuestionTemplate>(feedbackTemplate, delegate(QuestionTemplate s) { return s.Id == q.Id; });
                if (template == null)
                    return false;
                if (template.Prompt != q.Prompt)
                    return false;
                if (template.Type != q.Type)
                    return false;
                if (q.Answer.IsEmpty())
                    continue;
                if (template.Type == "radio" && !template.Options.Contains(q.Answer))
                    return false;
                if (template.Type == "text" && q.Answer.Length > 1000)
                    return false;
            }

            return true;
        }

    }

}