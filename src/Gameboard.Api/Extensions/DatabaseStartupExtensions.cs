// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Gameboard.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gameboard.Api.Extensions
{
    public static class DatabaseStartupExtensions
    {
        public static WebApplication InitializeDatabase(this WebApplication app, ILogger logger)
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var config = services.GetRequiredService<IConfiguration>();
                var env = services.GetService<IWebHostEnvironment>();

                using (var db = services.GetService<GameboardDbContext>())
                {
                    if (!db.Database.IsInMemory())
                    {
                        db.Database.Migrate();
                    }

                    SeedDatabase(env, config, db, logger);
                }

                return app;
            }
        }

        private static void SeedEnumerable<T>(this GameboardDbContext db, IEnumerable<T> entities, ILogger logger) where T : class, IEntity
        {
            foreach (var seedEntity in entities)
            {
                if (db.Set<T>().Any(e => e.Id == seedEntity.Id))
                {
                    logger.LogInformation($"Seeded {typeof(T).Name} {seedEntity.Id} skipped - already exists in the database.");
                    continue;
                }

                db.Set<T>().Add(seedEntity);
            }
        }

        private static void SeedDatabase(IWebHostEnvironment env, IConfiguration config, GameboardDbContext db, ILogger logger)
        {
            var configSeedFile = config.GetValue<string>("Database:SeedFile", null);
            if (string.IsNullOrWhiteSpace(configSeedFile))
                return;

            string seedFile = Path.Combine(
                env.ContentRootPath,
                configSeedFile
            );

            if (!File.Exists(seedFile))
            {
                logger.LogInformation(message: $"The current seed file ({seedFile}) doesn't exist, so no data will be seeded to this Gameboard installation.");
                return;
            }
            else { logger.LogInformation(message: $"Seeding data from {seedFile}..."); }

            var seedModel = LoadSeedModel(seedFile);

            db.SeedEnumerable(seedModel.Challenges, logger);
            db.SeedEnumerable(seedModel.ChallengeSpecs, logger);
            db.SeedEnumerable(seedModel.Feedback, logger);
            db.SeedEnumerable(seedModel.Games, logger);
            db.SeedEnumerable(seedModel.Players, logger);
            db.SeedEnumerable(seedModel.Sponsors, logger);
            db.SeedEnumerable(seedModel.Users, logger);

            logger.LogInformation($"Prepared to seed. Summary of changes: {db.ChangeTracker.DebugView.ShortView.Trim()}");
            db.SaveChanges();
            logger.LogInformation("Seeding complete.");
        }

        private static DbSeedModel LoadSeedModel(string seedFilePath)
        {
            var text = File.ReadAllText(seedFilePath);
            var extension = Path.GetExtension(seedFilePath).ToLower();
            DbSeedModel seedModel = null;

            switch (extension)
            {
                case ".json":
                    seedModel = JsonSerializer.Deserialize<DbSeedModel>(text, new JsonSerializerOptions()
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    break;
                case ".yaml":
                    var yamlDeserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance)
                        .IgnoreUnmatchedProperties()
                        .Build();

                    seedModel = yamlDeserializer.Deserialize<DbSeedModel>(File.ReadAllText(text));

                    break;
                default:
                    throw new InvalidDataException("Gameboard can only seed data from files in .yaml or .json format. Supply your seed data in one of these.");
            }

            return seedModel;
        }
    }
}
