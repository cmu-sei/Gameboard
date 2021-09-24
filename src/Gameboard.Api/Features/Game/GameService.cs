// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Gameboard.Api.Services
{
    public class GameService: _Service
    {
        IGameStore Store { get; }

        public GameService (
            ILogger<GameService> logger,
            IMapper mapper,
            CoreOptions options,
            IGameStore store
        ): base(logger, mapper, options)
        {
            Store = store;
        }

        public async Task<Game> Create(NewGame model)
        {
            var entity = Mapper.Map<Data.Game>(model);

            await Store.Create(entity);

            return Mapper.Map<Game>(entity);
        }

        public async Task<Game> Retrieve(string id)
        {
            return Mapper.Map<Game>(await Store.Retrieve(id));
        }

        public async Task Update(ChangedGame account)
        {
            var entity = await Store.Retrieve(account.Id);

            Mapper.Map(account, entity);

            await Store.Update(entity);
        }

        public async Task Delete(string id)
        {
            await Store.Delete(id);
        }

        public async Task<Game[]> List(GameSearchFilter model, bool sudo)
        {
            DateTimeOffset now = DateTimeOffset.Now;

            var q = Store.List(model.Term);

            if (!sudo)
                q = q.Where(g => g.IsPublished);

            if (model.WantsPresent)
                q = q.Where(g => g.GameEnd > now && g.GameStart < now);

            if (model.WantsFuture)
                q = q.Where(g => g.GameStart > now);

            if (model.WantsPast)
                q = q.Where(g => g.GameEnd < now);

            if (model.WantsPast)
                q = q.OrderByDescending(g => g.GameStart).ThenBy(g => g.Name);
            else
                q = q.OrderBy(g => g.GameStart).ThenBy(g => g.Name);

            q = q.Skip(model.Skip);

            if (model.Take > 0)
                q = q.Take(model.Take);

            return await Mapper.ProjectTo<Game>(q).ToArrayAsync();
        }

        public async Task<ChallengeSpec[]> RetrieveChallenges(string id)
        {
            var entity = await Store.Load(id);

            return Mapper.Map<ChallengeSpec[]>(
                entity.Specs
            );
        }

        public async Task<SessionForecast[]> SessionForecast(string id)
        {
            Data.Game entity = await Store.Retrieve(id);

            var ts = DateTimeOffset.UtcNow;
            var step = ts;

            var expirations = await Store.DbContext.Players
                .Where(p => p.GameId == id && p.Role == PlayerRole.Manager && p.SessionEnd.CompareTo(ts) > 0)
                .Select(p => p.SessionEnd)
                .ToArrayAsync();

            // foreach half hour, get count of available seats
            List<SessionForecast> result = new();

            for (int i = 0; i < 480; i+=30)
            {
                step = ts.AddMinutes(i);
                int reserved = expirations.Count(d => step.CompareTo(d) < 0);
                result.Add(new SessionForecast
                {
                    Time = step,
                    Reserved = reserved,
                    Available = entity.SessionLimit - reserved
                });
            }

            return result.ToArray();
        }

        public async Task<string> Export(GameSpecExport model)
        {
            var yaml = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var entity = await Store.Retrieve(model.Id, q => q.Include(g => g.Specs));

            if (entity is Data.Game)
                return yaml.Serialize(entity);

            entity = new Data.Game
            {
                Id = Guid.NewGuid().ToString("n")
            };

            for (int i = 0; i < model.GenerateSpecCount; i++)
                entity.Specs.Add(new Data.ChallengeSpec
                {
                    Id = Guid.NewGuid().ToString("n"),
                    GameId = entity.Id
                });

            return model.Format == ExportFormat.Yaml
                ? yaml.Serialize(entity)
                : JsonSerializer.Serialize(entity, JsonOptions)
            ;

        }

        public async Task<Game> Import(GameSpecImport model)
        {
            var yaml = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var entity = yaml.Deserialize<Data.Game>(model.Data);

            await Store.Create(entity);

            return Mapper.Map<Game>(entity);
        }

        public async Task UpdateImage(string id, string type, string filename)
        {
            var entity = await Store.Retrieve(id);

            switch (type)
            {
                case AppConstants.ImageMapType:
                entity.Background = filename;
                break;

                case AppConstants.ImageCardType:
                entity.Logo = filename;
                break;
            }

            await Store.Update(entity);
        }
    }

}
