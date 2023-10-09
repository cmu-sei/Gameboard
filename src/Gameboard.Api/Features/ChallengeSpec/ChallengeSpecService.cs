// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services;

public class ChallengeSpecService : _Service
{
    private readonly IStore _store;
    IGameEngineService GameEngine { get; }

    public ChallengeSpecService
    (
        ILogger<ChallengeSpecService> logger,
        IMapper mapper,
        CoreOptions options,
        IStore store,
        IGameEngineService gameEngine
    ) : base(logger, mapper, options)
    {
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

    public async Task<ExternalSpec[]> List(SearchFilter model)
    {
        return await GameEngine.ListSpecs(model);
    }

    public async Task<IEnumerable<BoardSpec>> ListGameSpecs(string gameId)
        => await Mapper.ProjectTo<BoardSpec>
        (
            _store
                .WithNoTracking<Data.ChallengeSpec>()
                .Where(s => s.GameId == gameId)
        ).ToArrayAsync();

    public async Task Sync(string id)
    {
        var externals = (await GameEngine.ListSpecs(new SearchFilter()))
                .ToDictionary(o => o.ExternalId);

        var specs = _store
            .WithTracking<Data.ChallengeSpec>()
            .Where(g => g.GameId == id);

        foreach (var spec in specs)
        {
            if (externals.ContainsKey(spec.ExternalId).Equals(false))
                continue;

            spec.Name = externals[spec.ExternalId].Name;
            spec.Description = externals[spec.ExternalId].Description;
            spec.Text = externals[spec.ExternalId].Text;
        }

        await _store.SaveUpdateRange(specs.ToArray());
    }
}
