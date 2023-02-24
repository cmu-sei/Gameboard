// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.GameEngine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services
{
    public class ChallengeSpecService : _Service
    {
        IChallengeSpecStore Store { get; }
        GameEngineService GameEngine { get; }

        public ChallengeSpecService(
            ILogger<ChallengeSpecService> logger,
            IMapper mapper,
            CoreOptions options,
            IChallengeSpecStore store,
            GameEngineService gameEngine
        ) : base(logger, mapper, options)
        {
            Store = store;
            GameEngine = gameEngine;
        }

        public async Task<ChallengeSpec> AddOrUpdate(NewChallengeSpec model)
        {
            var entity = await Store.List().FirstOrDefaultAsync(s =>
                s.ExternalId == model.ExternalId &&
                s.GameId == model.GameId
            );

            if (entity is Data.ChallengeSpec)
            {
                Mapper.Map(model, entity);
                await Store.Update(entity);
            }
            else
            {
                entity = Mapper.Map<Data.ChallengeSpec>(model);
                await Store.Create(entity);
            }

            return Mapper.Map<ChallengeSpec>(entity);
        }

        public async Task<ChallengeSpec> Retrieve(string id)
        {
            return Mapper.Map<ChallengeSpec>(await Store.Retrieve(id));
        }

        public async Task Update(ChangedChallengeSpec account)
        {
            var entity = await Store.Retrieve(account.Id);
            Mapper.Map(account, entity);

            await Store.Update(entity);
        }

        public async Task Delete(string id)
        {
            await Store.Delete(id);
        }

        public async Task<ExternalSpec[]> List(SearchFilter model)
        {
            return await GameEngine.ListSpecs(model);
        }

        public async Task Sync(string id)
        {
            var externals = (await List(new SearchFilter()))
                .ToDictionary(o => o.ExternalId)
            ;

            foreach (var spec in Store.DbSet.Where(s => s.GameId == id))
            {
                if (externals.ContainsKey(spec.ExternalId).Equals(false))
                    continue;

                spec.Name = externals[spec.ExternalId].Name;
                spec.Description = externals[spec.ExternalId].Description;
            }

            await Store.DbContext.SaveChangesAsync();
        }

        internal async Task<ChallengeSpecSummary[]> Browse(SearchFilter model)
        {
            var q = Store.List()
                .Include(s => s.Game)
                .Where(s => s.Game.PlayerMode == PlayerMode.Practice)
                .AsNoTracking()
            ;

            if (model.HasTerm)
            {
                string term = model.Term.ToLower();
                q = q.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    s.Description.ToLower().Contains(term) ||
                    s.Game.Name.ToLower().Contains(term)
                );
            }

            q = q.OrderBy(s => s.Name);

            q = q.Skip(model.Skip);

            if (model.Take > 0)
                q = q.Take(model.Take);

            return await Mapper.ProjectTo<ChallengeSpecSummary>(q).ToArrayAsync();
        }
    }
}
