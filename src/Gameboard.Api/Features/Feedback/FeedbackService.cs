// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services
{
    public class FeedbackService : _Service
    {
        IFeedbackStore FeedbackStore { get; }

        private IMemoryCache _localcache;
        private IStore _store;

        public FeedbackService(
            ILogger<FeedbackService> logger,
            IMapper mapper,
            CoreOptions options,
            IFeedbackStore feedbackStore,
            IStore store,
            IMemoryCache localcache
        ) : base(logger, mapper, options)
        {
            FeedbackStore = feedbackStore;
            _localcache = localcache;
            _store = store;
        }

        public async Task<Feedback> Retrieve(FeedbackSearchParams model, string actorId)
        {
            // for normal challenge and game feedback, we can just do simple lookups on the provided IDs.
            // unfortunately, for practice mode, we need a special case.
            // 
            // since a user can solve a practice challenge multiple times, we actually don't care about the
            // specific challengeID provided - we care if they've ever submitted feedback for a challenge
            // with the specified specId. If they have, that feedback applies to every instance of the spec
            // (challenge) theye've solved.
            // 
            // to manage this, we need to load the challenge (if they're asking for challenge-level feedback)
            // and determine if it's a practice challenge. if so, do special logic and leave.
            if (model.ChallengeId.IsNotEmpty())
            {
                var challenge = await _store
                    .WithNoTracking<Data.Challenge>()
                    .SingleAsync(c => c.Id == model.ChallengeId && c.SpecId == model.ChallengeSpecId);

                if (challenge.PlayerMode == PlayerMode.Practice)
                {
                    var feedback = await _store
                        .WithNoTracking<Data.Feedback>()
                        .Where
                        (
                            f =>
                                f.ChallengeSpecId == model.ChallengeSpecId &&
                                f.UserId == actorId
                        )
                        .SingleOrDefaultAsync();

                    return Mapper.Map<Feedback>(feedback);
                }
            }

            // if we get here, we're just doing standard lookups with no special logic
            var lookup = MakeFeedbackLookup(model.GameId, model.ChallengeId, model.ChallengeSpecId, actorId);
            var entity = await FeedbackStore.Load(lookup);
            return Mapper.Map<Feedback>(entity);
        }


        public async Task<Feedback> Submit(FeedbackSubmission model, string actorId)
        {
            var lookup = MakeFeedbackLookup(model.GameId, model.ChallengeId, model.ChallengeSpecId, actorId);
            var entity = await FeedbackStore.Load(lookup);

            if (model.Submit) // Only fully validate questions on submit as a slight optimization
            {
                var valid = await FeedbackMatchesTemplate(model.Questions, model.GameId, model.ChallengeId);
                if (!valid)
                    throw new InvalideFeedbackFormat();
            }

            if (entity is Data.Feedback)
            {
                if (entity.Submitted)
                {
                    return Mapper.Map<Feedback>(entity);
                }
                Mapper.Map(model, entity);
                entity.Timestamp = DateTimeOffset.UtcNow; // always last saved/submitted
                await FeedbackStore.Update(entity);
            }
            else // create new entity and assign player based on user/game combination
            {
                var player = await FeedbackStore.DbContext.Players.FirstOrDefaultAsync(s =>
                    s.UserId == actorId &&
                    s.GameId == model.GameId
                );
                if (player == null)
                    throw new ResourceNotFound<Data.Player>("Id from user/game", $"COuldn't find a player by game {model.GameId} and user {actorId}.");

                entity = Mapper.Map<Data.Feedback>(model);
                entity.UserId = actorId;
                entity.PlayerId = player.Id;
                entity.Id = Guid.NewGuid().ToString("n");
                entity.Timestamp = DateTimeOffset.UtcNow;
                await FeedbackStore.Create(entity);
            }

            return Mapper.Map<Feedback>(entity);
        }

        // List feedback responses based on params such as game/challenge filtering, skip/take, and sorting
        public async Task<FeedbackReportDetails[]> List(FeedbackSearchParams model)
        {
            var q = FeedbackStore.List(model.Term);

            if (model.GameId.NotEmpty())
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

        // Same as List() but ensures that query is not limited by take or skip
        public async Task<FeedbackReportDetails[]> ListFull(FeedbackSearchParams model)
        {
            model.Take = 0;
            model.Skip = 0;
            return await List(model);
        }

        // Supports turning feedback search params or feedback submission into model to lookup with Load()
        private Data.Feedback MakeFeedbackLookup(string gameId, string challengeId, string challengeSpecId, string userId)
        {
            return new Data.Feedback
            {
                GameId = gameId,
                ChallengeId = challengeId,
                ChallengeSpecId = challengeSpecId,
                UserId = userId
            };
        }

        // check that the actor/user is enrolled in this game they are trying to submit feedback for
        public async Task<bool> UserIsEnrolled(string gameId, string userId)
        {
            return await FeedbackStore.DbContext.Users.AnyAsync(u =>
                u.Id == userId &&
                u.Enrollments.Any(e => e.GameId == gameId)
            );
        }

        // Simple helper for returning the feedback template questions based on type of feedback
        public QuestionTemplate[] GetTemplate(bool wantsGame, Game game)
        {
            QuestionTemplate[] questionTemplate;
            if (wantsGame)
                questionTemplate = game.FeedbackTemplate.Game;
            else
                questionTemplate = game.FeedbackTemplate.Challenge;
            return questionTemplate;
        }

        // Maps report details to helper object to efficiently process individual questions based on question ids
        public FeedbackReportHelper[] MakeHelperList(FeedbackReportDetails[] feedback)
        {
            var result = Mapper.Map<FeedbackReportDetails[], FeedbackReportHelper[]>(feedback);
            return result;
        }

        // Given a submission of questions and a gameId, check that the questions match the game template and meet requirements
        private async Task<bool> FeedbackMatchesTemplate(QuestionSubmission[] feedback, string gameId, string challengeId)
        {
            var game = Mapper.Map<Game>(await FeedbackStore.DbContext.Games.FindAsync(gameId));

            var feedbackTemplate = GetTemplate(challengeId.IsEmpty(), game);

            if (feedbackTemplate.Length != feedback.Length)
                throw new InvalideFeedbackFormat();

            var templateMap = new Dictionary<string, QuestionTemplate>();
            foreach (QuestionTemplate q in feedbackTemplate) { templateMap.Add(q.Id, q); }

            foreach (var q in feedback)
            {
                var template = templateMap.GetValueOrDefault(q.Id, null);
                if (template == null) // user submitted id that isn't in game template
                    throw new InvalideFeedbackFormat();
                if (template.Required && q.Answer.IsEmpty()) // requirement config is not met
                    throw new MissingRequiredField();
                if (q.Answer.IsEmpty()) // don't validate answer is null/empty, if not required
                    continue;
                if (template.Type == "text" && q.Answer.Length > 2000) // universal character limit per text question 
                    throw new InvalideFeedbackFormat();
                if (template.Type == "likert") // because all likert options are ints, parse and check range with max config
                {
                    int answerInt;
                    bool isInt = Int32.TryParse(q.Answer, out answerInt);
                    if (!isInt || answerInt < template.Min || answerInt > template.Max) // parsing failed or outside of range
                        throw new InvalideFeedbackFormat();
                }
            }
            return true;
        }
    }
}
