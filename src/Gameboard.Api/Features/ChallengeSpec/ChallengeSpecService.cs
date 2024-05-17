// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

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

public class ChallengeSpecService : _Service
{
    private readonly INowService _now;
    private readonly ISlugService _slug;
    private readonly IStore _store;
    IGameEngineService GameEngine { get; }

    public ChallengeSpecService
    (
        ILogger<ChallengeSpecService> logger,
        IMapper mapper,
        INowService now,
        ISlugService slug,
        CoreOptions options,
        IStore store,
        IGameEngineService gameEngine
    ) : base(logger, mapper, options)
    {
        _now = now;
        _slug = slug;
        _store = store;
        GameEngine = gameEngine;
    }

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
                        entity.Tag = specSlug

                    // look, you can't win them all.
                }
            }

            await _store.Create(entity);
        }

        return Mapper.Map<ChallengeSpec>(entity);
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
        => GameEngine.ListSpecs(model);

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
            if (externalSpecs.ContainsKey(spec.ExternalId))
                SyncSpec(spec, externalSpecs[spec.ExternalId]);
        }

        await _store.SaveUpdateRange(activeSpecs);
    }

    internal async Task<IDictionary<string, ExternalSpec>> LoadExternalSpecsForSync()
    {
        return (await GameEngine.ListSpecs(new SearchFilter()))
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
