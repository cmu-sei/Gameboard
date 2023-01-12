// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.IO;
using System.Linq;
using System.Text.Json;
using Gameboard.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gameboard.Api.Extensions
{
    public static class DatabaseStartupExtensions
    {

        public static WebApplication InitializeDatabase(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var config = services.GetRequiredService<IConfiguration>();
                var env = services.GetService<IWebHostEnvironment>();
                var db = services.GetService<GameboardDbContext>();

                if (!db.Database.IsInMemory())
                {
                    db.Database.Migrate();
                }

                string seedFile = Path.Combine(
                    env.ContentRootPath,
                    config.GetValue<string>("Database:SeedFile", "seed-data.yaml")
                );

                var YamlDeserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                if (File.Exists(seedFile))
                {

                    DbSeedModel seedModel = Path.GetExtension(seedFile).ToLower() == "json"
                        ? JsonSerializer.Deserialize<DbSeedModel>(File.ReadAllText(seedFile))
                        : YamlDeserializer.Deserialize<DbSeedModel>(File.ReadAllText(seedFile));

                    foreach (var user in seedModel.Users)
                    {
                        if (db.Users.Any(u => u.Id == user.Id))
                            continue;

                        db.Users.Add(user);
                    }
                    db.SaveChanges();

                    foreach (var game in seedModel.Games)
                    {
                        if (db.Games.Any(u => u.Id == game.Id))
                            continue;

                        db.Games.Add(game);
                    }
                    db.SaveChanges();

                    foreach (var spec in seedModel.ChallengeSpecs)
                    {
                        if (db.ChallengeSpecs.Any(u => u.Id == spec.Id))
                            continue;

                        db.ChallengeSpecs.Add(spec);
                    }
                    db.SaveChanges();

                    foreach (var player in seedModel.Players)
                    {
                        if (db.Players.Any(u => u.Id == player.Id))
                            continue;

                        db.Players.Add(player);
                    }
                    db.SaveChanges();

                    foreach (var challenge in seedModel.Challenges)
                    {
                        if (db.Challenges.Any(u => u.Id == challenge.Id))
                            continue;

                        db.Challenges.Add(challenge);
                    }
                    db.SaveChanges();

                    foreach (var sponsor in seedModel.Sponsors)
                    {
                        if (db.Sponsors.Any(u => u.Id == sponsor.Id))
                            continue;

                        db.Sponsors.Add(sponsor);
                    }
                    db.SaveChanges();

                    foreach (var feedback in seedModel.Feedback)
                    {
                        if (db.Feedback.Any(u => u.Id == feedback.Id))
                            continue;

                        db.Feedback.Add(feedback);
                    }
                    db.SaveChanges();
                }

                return app;
            }
        }
    }
}
