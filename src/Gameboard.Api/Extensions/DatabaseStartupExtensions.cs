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
        public static WebApplication InitializeDatabase(this WebApplication app, AppSettings settings, ILogger logger)
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

                    SeedDatabase(env, config, db, settings, logger);
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

        private static void SeedDatabase(IWebHostEnvironment env, IConfiguration config, GameboardDbContext db, AppSettings settings, ILogger logger)
        {
            // first, seed the admin user configured in appsettings.config
            if (!string.IsNullOrWhiteSpace(settings.Database.AdminId))
            {
                logger.LogInformation($"Admin user '{settings.Database.AdminName}' found in configuration. Seeding now...");

                if (db.Users.FirstOrDefault(u => u.Id == settings.Database.AdminId) != null)
                {
                    logger.LogInformation("This user is already seeded in the database. Skipping it this time.");
                }
                else
                {
                    db.Users.Add(new Data.User
                    {
                        Id = settings.Database.AdminId,
                        Username = "gb-admin",
                        ApprovedName = settings.Database.AdminName,
                        Role = settings.Database.AdminRole
                    });
                }
            }

            var configSeedFile = config.GetValue<string>("Database:SeedFile", null);
            var isSeedFileConfigured = !string.IsNullOrWhiteSpace(configSeedFile);
            if (!isSeedFileConfigured)
            {
                logger.LogInformation("No seed file configured.");
            }

            string seedFilePath = isSeedFileConfigured ? Path.Combine(env.ContentRootPath, configSeedFile) : "";
            var seedFileExists = File.Exists(seedFilePath);
            if (!seedFileExists)
            {
                logger.LogInformation(message: $"The current seed file ({seedFilePath}) doesn't exist.");
            }

            if (isSeedFileConfigured && seedFileExists)
            {
                logger.LogInformation($"Seeding data from {seedFilePath}...");
                var seedModel = LoadSeedModel(seedFilePath);

                db.SeedEnumerable(seedModel.Challenges, logger);
                db.SeedEnumerable(seedModel.ChallengeSpecs, logger);
                db.SeedEnumerable(seedModel.Feedback, logger);
                db.SeedEnumerable(seedModel.Games, logger);
                db.SeedEnumerable(seedModel.Players, logger);
                db.SeedEnumerable(seedModel.Sponsors, logger);
                db.SeedEnumerable(seedModel.Users, logger);
            }

            if (db.ChangeTracker.HasChanges())
            {
                logger.LogInformation($"Prepared to seed. Summary of changes: {db.ChangeTracker.DebugView.ShortView.Trim()}");
                db.SaveChanges();
                logger.LogInformation("Seeding complete.");
            }
            else
            {
                logger.LogInformation("No data seeded.");
            }
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
