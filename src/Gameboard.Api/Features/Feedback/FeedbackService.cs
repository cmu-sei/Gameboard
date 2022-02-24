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

            var entity = await Store.List()
                .FirstOrDefaultAsync(s =>
                    s.ChallengeSpecId == model.ChallengeSpecId &&
                    s.ChallengeId == model.ChallengeId &&
                    s.UserId == actorId &&
                    s.GameId == model.GameId
                );

            if (model.Submit) // Only fully validate questions on submit as a slight optimization
            { 
                var valid = await FeedbackMatchesTemplate(model.Questions, model.GameId, model.ChallengeId);
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
            else // create new entity and 
            {
                var player = await Store.DbContext.Players.FirstOrDefaultAsync(s =>
                    s.UserId == actorId &&
                    s.GameId == model.GameId
                );
                if (player == null)
                    throw new ResourceNotFound();

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
            
            if (model.WantsGame)
                q = q.Where(u => u.ChallengeSpecId == null);
            else
                q = q.Where(u => u.ChallengeSpecId != null);

            if (model.WantsSpecificChallenge)
                q = q.Where(u => u.ChallengeSpecId == model.ChallengeSpecId);

            if (model.WantsSubmittedOnly)
                q = q.Where(u => u.Submitted);

            if (model.WantsSortByTimeNewest)
                q = q.OrderByDescending(u => u.Timestamp);
            else if (model.WantsSortByTimeOldest)
                q = q.OrderBy(u => u.Timestamp);

            q = q.Include(p => p.Player).Include(p => p.ChallengeSpec);

            q = q.Skip(model.Skip);

            if (model.Take > 0)
                q = q.Take(model.Take);

            return await Mapper.ProjectTo<FeedbackReportDetails>(q).ToArrayAsync();
        }

        public async Task<Feedback[]> ListBySpec(string specId)
        {
            var q = Store.List();

            q = q.Where(u => u.ChallengeSpecId == specId);

            return await Mapper.ProjectTo<Feedback>(q).ToArrayAsync();
        }

        public async Task<Feedback[]> ListByGame(string gameId, bool challengeOnly)
        {
            var q = Store.List();

            q = q.Where(u => u.GameId == gameId && u.ChallengeSpecId == null);

            return await Mapper.ProjectTo<Feedback>(q).ToArrayAsync();
        }

        public async Task<string> ResolveApiKey(string key)
        {
            if (key.IsEmpty())
                return null;

            var entity = await Store.ResolveApiKey(key.ToSha256());

            return entity?.Id;
        }

        public async Task<bool> UserIsEnrolled(string gameId, string userId)
        {
            return await Store.DbContext.Users.AnyAsync(u =>
                u.Id == userId &&
                u.Enrollments.Any(e => e.GameId == gameId)
            );
        }

         public FeedbackReportHelper[] MakeHelperList(FeedbackReportDetails[] feedback)
        {
            var result = Mapper.Map<FeedbackReportDetails[], FeedbackReportHelper[]>(feedback);
            return result;
        }

        private async Task<bool> FeedbackMatchesTemplate(QuestionSubmission[] feedback, string gameId, string challengeId) {
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
                // if (template.Prompt != q.Prompt)
                //     return false;
                // if (template.Type != q.Type)
                //     return false;
                if (template.Required && q.Answer.IsEmpty())
                    return false;
                if (q.Answer.IsEmpty())
                    continue;
                if (template.Type == "text" && q.Answer.Length > 4000)
                    return false;
                if (template.Type == "likert" )
                {
                    int answerInt;
                    bool isInt = Int32.TryParse(q.Answer, out answerInt);
                    if (!isInt || answerInt < template.Min || answerInt > template.Max)
                        return false;
                }
                    
            }

            return true;
        }

    }

}