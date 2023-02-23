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
        Defaults Defaults { get; }
        public GameService (
            ILogger<GameService> logger,
            IMapper mapper,
            CoreOptions options,
            Defaults defaults,
            IGameStore store
        ): base(logger, mapper, options)
        {
            Store = store;
            Defaults = defaults;
        }

        public async Task<Game> Create(NewGame model)
        {
            // for "New Game" only, set global defaults, if defined
            if (!model.IsClone)
            {   if (Defaults.FeedbackTemplate.NotEmpty())
                    model.FeedbackConfig = Defaults.FeedbackTemplate;
                if (Defaults.CertificateTemplate.NotEmpty())
                    model.CertificateTemplate = Defaults.CertificateTemplate;
            }

            var entity = Mapper.Map<Data.Game>(model);

            await Store.Create(entity);

            return Mapper.Map<Game>(entity);
        }

        public async Task<Game> Retrieve(string id, bool accessHidden = true)
        {
            var game = await Store.Retrieve(id);
            if (!accessHidden && !game.IsPublished)
                throw new ActionForbidden();
            return Mapper.Map<Game>(game);
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
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var q = Store.List(model.Term);

            if (!sudo)
                q = q.Where(g => g.IsPublished);

            if (model.WantsPresent)
                q = q.Where(g => g.GameEnd > now && g.GameStart < now);

            if (model.WantsFuture)
                q = q.Where(g => g.GameStart > now);

            if (model.WantsPast)
                q = q.Where(g => g.GameEnd < now);

            if (model.WantsFuture)
                q = q.OrderBy(g => g.GameStart).ThenBy(g => g.Name);
            else
                q = q.OrderByDescending(g => g.GameStart).ThenBy(g => g.Name);

            q = q.Skip(model.Skip);

            if (model.Take > 0)
                q = q.Take(model.Take);

            // Use Map instead of 'Mapper.ProjectTo<Game>' to support YAML parsing in automapper
            return Mapper.Map<Game[]>(await q.ToArrayAsync());
        }

        public async Task<GameGroup[]> ListGrouped(GameSearchFilter model, bool sudo)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var q = Store.List(model.Term);

            if (!sudo)
                q = q.Where(g => g.IsPublished);

            if (model.WantsPresent)
                q = q.Where(g => g.GameEnd > now && g.GameStart < now);
            if (model.WantsFuture)
                q = q.Where(g => g.GameStart > now);
            if (model.WantsPast)
                q = q.Where(g => g.GameEnd < now);

            var games = await q.ToArrayAsync();

            var b = games
                .GroupBy(g => new
                {
                    g.GameStart.Year,
                    g.GameStart.Month,
                })
                .Select(g => new GameGroup
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Games = g
                        .OrderBy(c => c.GameStart)
                        .Select(c =>  Mapper.Map<Game>(c))
                        .ToArray()
                });

            if (model.WantsPast)
                b = b.OrderByDescending(g => g.Year).ThenByDescending(g => g.Month);
            else
                b = b.OrderBy(g => g.Year).ThenBy(g => g.Month);

            return b.ToArray();
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

        public async Task ReRank(string id)
        {
            var players = await Store.DbContext.Players
                .Where(p => p.GameId == id && p.Mode == PlayerMode.Competition)
                .OrderByDescending(p => p.Score)
                .ThenBy(p => p.Time)
                .ThenByDescending(p => p.CorrectCount)
                .ThenByDescending(p => p.PartialCount)
                .ToArrayAsync()
            ;

            int rank = 0;
            string last = "";
            foreach (var player in players)
            {
                if (player.TeamId != last)
                {
                    rank += 1;
                    last = player.TeamId;
                }

                player.Rank = rank;
            }

            await Store.DbContext.SaveChangesAsync();
        }

        public async Task<bool> UserIsTeamPlayer(string uid, string gid, string tid)
        {
            bool authd = await Store.DbContext.Users.AnyAsync(u =>
                u.Id == uid &&
                u.Enrollments.Any(e => e.TeamId == tid)
            );

            var players = Store.DbContext.Players.Where(p => p.UserId == uid);
            foreach (var e in players) {
                Console.WriteLine("game id: " + e.GameId + " | gid: " + gid);
            }

            return authd;
        }

        /*
        public async Task<bool> UserIsTeamPlayer(string id, string subjectId)
        {
            var entity = await Store.Retrieve(id);

            return await Store.DbContext.Users.AnyAsync(u =>
                u.Id == subjectId &&
                u.Enrollments.Any(e => e.TeamId == entity.TeamId)
            );
        }
        */
    }

}
