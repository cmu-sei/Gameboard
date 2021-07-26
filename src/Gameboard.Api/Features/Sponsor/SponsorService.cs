// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Gameboard.Api.Services
{
    public class SponsorService: _Service
    {
        ISponsorStore Store { get; }

        public SponsorService (
            ILogger<SponsorService> logger,
            IMapper mapper,
            CoreOptions options,
            ISponsorStore store
        ): base (logger, mapper, options)
        {
            Store = store;
        }

        public async Task<Sponsor> Create(NewSponsor model)
        {
            var entity = Mapper.Map<Data.Sponsor>(model);
            await Store.Create(entity);
            return Mapper.Map<Sponsor>(entity);
        }

        public async Task<Sponsor> Retrieve(string id)
        {
            return Mapper.Map<Sponsor>(await Store.Retrieve(id));
        }

        public async Task AddOrUpdate(ChangedSponsor model)
        {
            var entity = await Store.Retrieve(model.Id);

            if (entity is not null)
            {
                Mapper.Map(model, entity);
                await Store.Update(entity);
                return;
            }

            entity = Mapper.Map<Data.Sponsor>(model);
            await Store.Create(entity);
        }

        public async Task Delete(string id)
        {
            var entity = await Store.Retrieve(id);

            await Store.Delete(id);

            if (entity.Logo.IsEmpty())
                return;

            string path = Path.Combine(Options.ImageFolder, entity.Logo);

            if (File.Exists(path))
                File.Delete(path);
        }

        public async Task<Sponsor[]> List(SearchFilter model)
        {
            var q = Store.List(model.Term);

            q = q.OrderBy(p => p.Id);

            q = q.Skip(model.Skip);

            if (model.Take > 0)
                q = q.Take(model.Take);

            return await Mapper.ProjectTo<Sponsor>(q).ToArrayAsync();
        }

        public async Task<Sponsor> AddOrUpdate(string id, string filename)
        {
            var entity = await Store.Retrieve(id);

            if (entity is null)
            {
                entity = await Store.Create(new Data.Sponsor{ Id = id });
            }

            entity.Logo = filename;

            await Store.Update(entity);

            return Mapper.Map<Sponsor>(entity);
        }
    }

}
