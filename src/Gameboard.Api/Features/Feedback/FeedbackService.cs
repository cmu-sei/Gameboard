// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gameboard.Api.Services;

public class FeedbackService
(
    IJsonService json,
    ILogger<FeedbackService> logger,
    IMapper mapper,
    CoreOptions options,
    IStore store
) : _Service(logger, mapper, options)
{
    private readonly IJsonService _json = json;
    private readonly IStore _store = store;

    public FeedbackQuestionsConfig BuildQuestionConfigFromTemplate(FeedbackTemplate template)
    {
        var yaml = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return yaml.Deserialize<FeedbackQuestionsConfig>(template.Content);
    }

    public async Task<int> GetFeedbackMaxResponses(FeedbackSearchParams model)
    {
        var total = 0;

        if (model.WantsGame) // count enrollments for a specific game id, that are started
            total = await _store
                .WithNoTracking<Data.Player>()
                .Where(p => p.GameId == model.GameId && p.SessionBegin > DateTimeOffset.MinValue)
                .CountAsync();
        else if (model.WantsSpecificChallenge) // count challenges with specific challenge spec id
            total = await _store
                .WithNoTracking<Data.Challenge>()
                .Where(p => p.SpecId == model.ChallengeSpecId)
                .CountAsync();
        else if (model.WantsChallenge) // count challenges with specific game id
            total = await _store
                .WithNoTracking<Data.Challenge>()
                .Where(p => p.GameId == model.GameId)
                .CountAsync();

        return total;
    }

    // Compute aggregates for each feedback question in template based on all responses in feedback table
    public IEnumerable<QuestionStats> GetFeedbackQuestionStats(QuestionTemplate[] questionTemplate, FeedbackReportHelper[] feedbackTable)
    {
        var questionStats = new List<QuestionStats>();
        foreach (QuestionTemplate question in questionTemplate)
        {
            if (question.Type != "likert")
                continue;

            var answers = new List<int>();
            foreach (var response in feedbackTable.Where(f => f.Submitted || true))
            {
                var answer = response.IdToAnswer.GetValueOrDefault(question.Id, null);
                if (answer != null)
                    answers.Add(Int32.Parse(answer));
            }
            var newStat = new QuestionStats
            {
                Id = question.Id,
                Prompt = question.Prompt,
                ShortName = question.ShortName,
                Required = question.Required,
                ScaleMin = question.Min,
                ScaleMax = question.Max,
                Count = answers.Count,
            };
            if (newStat.Count > 0)
            {
                newStat.Average = answers.Average();
                newStat.Lowest = answers.Min();
                newStat.Highest = answers.Max();
            }
            questionStats.Add(newStat);
        }
        return questionStats;
    }

    public async Task<FeedbackTemplate> ResolveTemplate(FeedbackSubmissionAttachedEntityType type, string id, CancellationToken cancellationToken)
    {
        var gameId = id;
        if (type == FeedbackSubmissionAttachedEntityType.ChallengeSpec)
        {
            gameId = await _store
                .WithNoTracking<Data.ChallengeSpec>()
                .Where(s => s.Id == id)
                .Select(s => s.GameId)
                .SingleAsync(cancellationToken);
        }

        var gameTemplates = await _store
            .WithNoTracking<Data.Game>()
            .Where(g => g.Id == gameId)
            .Select(g => new
            {
                Game = g.FeedbackTemplate,
                Challenges = g.ChallengesFeedbackTemplate
            })
            .SingleAsync(cancellationToken);

        return type == FeedbackSubmissionAttachedEntityType.Game ? gameTemplates.Game : gameTemplates.Challenges;
    }

    public async Task<Features.Feedback.Feedback> Retrieve(FeedbackSearchParams model, string actorId)
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

                return Mapper.Map<Features.Feedback.Feedback>(feedback);
            }
        }

        // if we get here, we're just doing standard lookups with no special logic
        var lookup = MakeFeedbackLookup(model.GameId, model.ChallengeId, model.ChallengeSpecId, actorId);
        var entity = await LoadFeedback(lookup);
        return Mapper.Map<Features.Feedback.Feedback>(entity);
    }

    public async Task<FeedbackSubmissionView> ResolveExistingSubmission(string userId, FeedbackSubmissionAttachedEntityType entityType, string entityId, CancellationToken cancellationToken)
    {
        IQueryable<FeedbackSubmission> query = null;
        if (entityType == FeedbackSubmissionAttachedEntityType.ChallengeSpec)
        {
            query = _store
                .WithNoTracking<FeedbackSubmissionChallengeSpec>()
                .Where(s => s.ChallengeSpecId == entityId);
        }
        else if (entityType == FeedbackSubmissionAttachedEntityType.Game)
        {
            query = _store
                .WithNoTracking<FeedbackSubmissionGame>()
                .Where(s => s.GameId == entityId);
        }

        if (query is null)
        {
            throw new NotImplementedException();
        }

        return await query
            .Include(s => s.User)
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.WhenCreated)
            .Select(s => new FeedbackSubmissionView
            {
                Id = s.Id,
                FeedbackTemplate = new SimpleEntity { Id = s.FeedbackTemplateId, Name = s.FeedbackTemplate.Name },
                Responses = s.Responses,
                User = new SimpleEntity { Id = s.UserId, Name = s.User.ApprovedName },
                WhenCreated = s.WhenCreated,
                WhenEdited = s.WhenEdited,
                WhenFinalized = s.WhenFinalized
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Features.Feedback.Feedback> Submit(FeedbackSubmissionLegacy model, string actorId)
    {
        var lookup = MakeFeedbackLookup(model.GameId, model.ChallengeId, model.ChallengeSpecId, actorId);
        var entity = await LoadFeedback(lookup);

        if (model.Submit) // Only fully validate questions on submit as a slight optimization
        {
            var valid = await FeedbackMatchesTemplate(model.Questions, model.GameId, model.ChallengeId);
            if (!valid)
                throw new InvalidFeedbackFormat();
        }

        if (entity is not null)
        {
            if (entity.Submitted)
            {
                return Mapper.Map<Features.Feedback.Feedback>(entity);
            }
            Mapper.Map(model, entity);
            entity.Timestamp = DateTimeOffset.UtcNow; // always last saved/submitted
            await _store.SaveUpdate(entity, CancellationToken.None);
        }
        else // create new entity and assign player based on user/game combination
        {
            var player = await _store.WithNoTracking<Data.Player>().FirstOrDefaultAsync(s =>
                s.UserId == actorId &&
                s.GameId == model.GameId
            ) ?? throw new ResourceNotFound<Data.Player>("Id from user/game", $"COuldn't find a player by game {model.GameId} and user {actorId}.");

            entity = Mapper.Map<Data.Feedback>(model);
            entity.UserId = actorId;
            entity.PlayerId = player.Id;
            entity.Id = Guid.NewGuid().ToString("n");
            entity.Timestamp = DateTimeOffset.UtcNow;
            await _store.Create(entity);
        }

        return Mapper.Map<Features.Feedback.Feedback>(entity);
    }

    // List feedback responses based on params such as game/challenge filtering, skip/take, and sorting
    public async Task<FeedbackReportDetails[]> List(FeedbackSearchParams model)
    {
        var q = _store.WithNoTracking<Data.Feedback>();

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

    public IQueryable<Data.Feedback> BuildQuery(FeedbackSearchParams args)
    {
        var q = _store.WithNoTracking<Data.Feedback>();

        if (args.GameId.NotEmpty())
            q = q.Where(u => u.GameId == args.GameId);

        if (args.WantsGame)
            q = q.Where(u => u.ChallengeSpecId == null);
        else
            q = q.Where(u => u.ChallengeSpecId != null);

        if (args.WantsSpecificChallenge)
            q = q.Where(u => u.ChallengeSpecId == args.ChallengeSpecId);

        if (args.WantsSubmittedOnly)
            q = q.Where(u => u.Submitted);

        if (args.WantsSortByTimeNewest)
            q = q.OrderByDescending(u => u.Timestamp);
        else if (args.WantsSortByTimeOldest)
            q = q.OrderBy(u => u.Timestamp);

        q = q.Include(p => p.Player).Include(p => p.ChallengeSpec);

        q = q.Skip(args.Skip);
        if (args.Take > 0)
            q = q.Take(args.Take);

        return q;
    }

    // check that the actor/user is enrolled in this game they are trying to submit feedback for
    public async Task<bool> UserIsEnrolled(string gameId, string userId)
    {
        return await _store.WithNoTracking<Data.User>().AnyAsync(u =>
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
        var game = Mapper.Map<Game>(await _store.WithNoTracking<Data.Game>().SingleOrDefaultAsync(g => g.Id == gameId));

        var feedbackTemplate = GetTemplate(challengeId.IsEmpty(), game);

        if (feedbackTemplate.Length != feedback.Length)
            throw new InvalidFeedbackFormat();

        var templateMap = new Dictionary<string, QuestionTemplate>();
        foreach (QuestionTemplate q in feedbackTemplate) { templateMap.Add(q.Id, q); }

        foreach (var q in feedback)
        {
            var template = templateMap.GetValueOrDefault(q.Id, null);
            if (template == null) // user submitted id that isn't in game template
                throw new InvalidFeedbackFormat();
            if (template.Required && q.Answer.IsEmpty()) // requirement config is not met
                throw new MissingRequiredField();
            if (q.Answer.IsEmpty()) // don't validate answer is null/empty, if not required
                continue;
            if (template.Type == "text" && q.Answer.Length > 2000) // universal character limit per text question 
                throw new InvalidFeedbackFormat();
            if (template.Type == "likert") // because all likert options are ints, parse and check range with max config
            {
                int answerInt;
                bool isInt = Int32.TryParse(q.Answer, out answerInt);
                if (!isInt || answerInt < template.Min || answerInt > template.Max) // parsing failed or outside of range
                    throw new InvalidFeedbackFormat();
            }
        }
        return true;
    }

    private Task<Data.Feedback> LoadFeedback(Data.Feedback feedbackLookup)
    {
        return _store
            .WithNoTracking<Data.Feedback>()
            .FirstOrDefaultAsync(s =>
                s.ChallengeSpecId == feedbackLookup.ChallengeSpecId &&
                s.ChallengeId == feedbackLookup.ChallengeId &&
                s.UserId == feedbackLookup.UserId &&
                s.GameId == feedbackLookup.GameId
            );
    }
}
