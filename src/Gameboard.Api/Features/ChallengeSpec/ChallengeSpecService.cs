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
using Gameboard.Api.Features.GameEngine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services;

public class ChallengeSpecService
(
    IJsonService jsonService,
    ILogger<ChallengeSpecService> logger,
    IMapper mapper,
    INowService now,
    ISlugService slug,
    CoreOptions options,
    Random random,
    IStore store,
    IGameEngineService gameEngine
    ) : _Service(logger, mapper, options)
{
    private readonly IGameEngineService _gameEngine = gameEngine;
    private readonly IJsonService _jsonService = jsonService;
    private readonly INowService _now = now;
    private readonly Random _random = random;
    private readonly ISlugService _slug = slug;
    private readonly IStore _store = store;

    public async Task<ChallengeSpec> AddOrUpdate(NewChallengeSpec model)
    {
        var entity = await _store
            .WithTracking<Data.ChallengeSpec>()
            .FirstOrDefaultAsync
            (
                s =>
                    s.ExternalId == model.ExternalId &&
                    s.GameId == model.GameId
            );

        if (entity is not null)
        {
            Mapper.Map(model, entity);
            await _store.SaveUpdate(entity, CancellationToken.None);
        }
        else
        {
            entity = Mapper.Map<Data.ChallengeSpec>(model);

            // default semi-random coords so they don't stack up
            if (entity.X == 0 && entity.Y == 0)
            {
                entity.X = (float)Decimal.Divide(_random.Next(50) + 25, 100);
                entity.Y = (float)Decimal.Divide(_random.Next(50) + 25, 100);
            }

            // point value must be non-negative and defaults to 100
            if (entity.Points <= 0)
            {
                entity.Points = 100;
            }

            if (entity.Tag.IsEmpty())
            {
                var gameTags = await _store
                    .WithNoTracking<Data.ChallengeSpec>()
                    .Where(s => s.GameId == model.GameId)
                    .Select(s => s.Tag)
                    .ToArrayAsync(CancellationToken.None);
                var specSlug = _slug.Get(entity.Name);

                // try to compose a random tag for the spec if one hasn't been supplied by the client
                if (!await _store.WithNoTracking<Data.ChallengeSpec>().AnyAsync(s => s.GameId == model.GameId && s.Tag == specSlug))
                    entity.Tag = specSlug;
                else
                {
                    var nowish = _now.Get();
                    specSlug = $"{specSlug}-{nowish.Year}{nowish.Month}{nowish.Day}{nowish.Millisecond}";

                    if (!gameTags.Any(s => s == specSlug))
                        entity.Tag = specSlug;

                    // look, you can't win them all.
                }
            }

            await _store.Create(entity);
        }

        return Mapper.Map<ChallengeSpec>(entity);
    }

    public Task<GameChallengeSpecs[]> ListByGame(string gameId = null)
        => _store
            .WithNoTracking<Data.ChallengeSpec>()
            .Where(s => !s.IsHidden)
            .Where(s => gameId == null || s.GameId == gameId)
            .Select(s => new
            {
                ChallengeSpec = new SimpleEntity
                {
                    Id = s.Id,
                    Name = s.Name
                },
                Game = new SimpleEntity { Id = s.GameId, Name = s.Game.Name },
            })
            .GroupBy(s => s.Game)
            .Select(gr => new GameChallengeSpecs { Game = gr.Key, ChallengeSpecs = gr.Select(thing => thing.ChallengeSpec).OrderBy(s => s.Name).ToArray() })
            .OrderBy(v => v.Game.Name)
            .ToArrayAsync();

    public async Task<IOrderedEnumerable<ChallengeSpecQuestionPerformance>> GetQuestionPerformance(string challengeSpecId, CancellationToken cancellationToken)
    {
        var results = await GetQuestionPerformance([challengeSpecId], cancellationToken);

        return results[challengeSpecId];
    }

    public async Task<IDictionary<string, IOrderedEnumerable<ChallengeSpecQuestionPerformance>>> GetQuestionPerformance(IEnumerable<string> challengeSpecIds, CancellationToken cancellationToken)
    {
        // pull raw data
        var data = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => challengeSpecIds.Contains(c.SpecId))
            .Select(c => new
            {
                c.Id,
                c.SpecId,
                c.Points,
                c.Score,
                c.StartTime,
                c.State
            })
            .GroupBy(c => c.SpecId)
            .ToDictionaryAsync(gr => gr.Key, gr => gr.Select(c => new
            {
                IsComplete = c.Points >= c.Score,
                IsPartial = c.Points < c.Score && c.Points > 0,
                IsZero = c.Score == 0,
                c.StartTime,
                c.State,
            }), cancellationToken);

        // because of topo architecture, we can't tell the caller anything about the challenge unless it's been attempted
        // at least once
        if (data is null || !data.Values.Any(v => v.Any()))
        {
            return challengeSpecIds.ToDictionary
            (
                specId => specId,
                specId => Array.Empty<ChallengeSpecQuestionPerformance>().OrderBy(c => c.CountCorrect)
            );
        }

        // deserialize states
        var questionPerformance = new Dictionary<string, IOrderedEnumerable<ChallengeSpecQuestionPerformance>>();
        foreach (var challengeSpecId in data.Keys)
        {
            var challenges = data[challengeSpecId]
                .OrderByDescending(c => c.StartTime)
                .Select(c => new ChallengeSpecQuestionPerformanceChallenge
                {
                    IsComplete = c.IsComplete,
                    IsPartial = c.IsPartial,
                    IsZero = c.IsZero,
                    State = _jsonService.Deserialize<GameEngineGameState>(c.State)
                });

            var allQuestions = challenges
                .Select(c => c.State)
                .Select(s => s.Challenge)
                .SelectMany(c => c.Questions)
                .GroupBy(q => q.Text)
                .ToDictionary(q => q.Key, q => q.ToArray());

            if (!challenges.Any())
                continue;

            var exemplarState = challenges.First().State;
            var maxWeight = exemplarState.Challenge.Questions.Select(q => q.Weight).Sum();
            var questions = exemplarState.Challenge.Questions.Select((q, index) => new ChallengeSpecQuestionPerformance
            {
                QuestionRank = index + 1,
                Hint = q.Hint,
                Prompt = q.Text,
                // it's apparently a topo rule that weight can be zero and that means that the challenge weight is equally divided
                PointValue = maxWeight == 0 ? (exemplarState.Challenge.MaxPoints / exemplarState.Challenge.Questions.Count()) : exemplarState.Challenge.MaxPoints * (q.Weight / maxWeight),
                CountCorrect = allQuestions[q.Text].Count(answeredQ => answeredQ.IsCorrect),
                CountSubmitted = allQuestions[q.Text].Count(answeredQ => answeredQ.IsGraded)
            }).OrderBy(q => q.QuestionRank);

            questionPerformance.Add(challengeSpecId, questions);
        }

        return questionPerformance;
    }

    public async Task<ChallengeSpec> Retrieve(string id)
        => Mapper.Map<ChallengeSpec>(await _store.FirstOrDefaultAsync<Data.ChallengeSpec>(s => s.Id == id, CancellationToken.None));

    public async Task Update(ChangedChallengeSpec spec)
    {
        var entity = await _store.SingleAsync<Data.ChallengeSpec>(spec.Id, CancellationToken.None); ;
        Mapper.Map(spec, entity);

        await _store.SaveUpdate(entity, CancellationToken.None);
    }

    public Task Delete(string id)
        => _store.Delete<Data.ChallengeSpec>(id);

    public Task<ExternalSpec[]> List(SearchFilter model)
        => _gameEngine.ListGameEngineSpecs(model);

    public async Task<IEnumerable<BoardSpec>> ListGameSpecs(string gameId)
        => await Mapper.ProjectTo<BoardSpec>
        (
            _store
                .WithNoTracking<Data.ChallengeSpec>()
                .Where(s => s.GameId == gameId)
        ).ToArrayAsync();

    public async Task Sync(string id)
    {
        var externals = await LoadExternalSpecsForSync();

        var specs = _store
            .WithTracking<Data.ChallengeSpec>()
            .Where(g => g.GameId == id);

        foreach (var spec in specs)
        {
            if (externals.ContainsKey(spec.ExternalId).Equals(false))
                continue;

            SyncSpec(spec, externals[spec.ExternalId]);
        }

        await _store.SaveUpdateRange(specs.ToArray());
    }

    /// <summary>
    /// Updates "active" challenge specs with information from the appropriate
    /// game engine (for now, only Topomojo.)
    /// 
    /// "Active" here is defined as specs that are used by a game with a current
    /// execution period or practice mode specs.
    /// </summary>
    /// <returns></returns>
    public async Task SyncActiveSpecs(CancellationToken cancellationToken)
    {
        var nowish = _now.Get();
        var activeSpecs = await _store
            .WithTracking<Data.ChallengeSpec>()
            .Where(s => s.Game.GameEnd > nowish || s.Game.PlayerMode == PlayerMode.Practice)
            .ToArrayAsync(cancellationToken);

        var externalSpecs = await LoadExternalSpecsForSync();

        foreach (var spec in activeSpecs)
        {
            if (externalSpecs.TryGetValue(spec.ExternalId, out ExternalSpec value))
                SyncSpec(spec, value);
        }

        await _store.SaveUpdateRange(activeSpecs);
    }

    internal async Task<IDictionary<string, ExternalSpec>> LoadExternalSpecsForSync()
    {
        return (await _gameEngine.ListGameEngineSpecs(new SearchFilter()))
            .ToDictionary(o => o.ExternalId);
    }

    internal void SyncSpec(Data.ChallengeSpec spec, ExternalSpec externalSpec)
    {
        spec.Name = externalSpec.Name;
        spec.Description = externalSpec.Description;
        spec.Tags = externalSpec.Tags;
        spec.Text = externalSpec.Text;
    }
}
