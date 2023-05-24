// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Gameboard.Api.Data;
using Gameboard.Api.Structure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

internal static class DataStartupExtensions
{
    public static IServiceCollection AddGameboardData(this IServiceCollection services, string provider, GameboardDataStoreConfig config)
    {
        services.AddSingleton(config);

        switch (provider.ToLower())
        {
            case "sqlserver":
                services.AddDbContext<GameboardDbContext, GameboardDbContextSqlServer>
                (
                    builder => builder
                        .UseSqlServer(config.ConnectionString)
                        .ConfigureDbContextOptions(config)
                );
                break;

            case "postgresql":
                services.AddDbContext<GameboardDbContext, GameboardDbContextPostgreSQL>(builder => builder.UseGameboardPostgreSql(config));
                break;

            default:
                services.AddDbContext<GameboardDbContext, GameboardDbContextInMemory>(
                    builder => builder
                        .UseInMemoryDatabase("Gameboard_Db")
                        .ConfigureDbContextOptions(config)
                );
                break;
        }

        // load stores by reflection
        foreach (var t in Assembly
            .GetExecutingAssembly()
            .DefinedTypes
            .Where(t =>
                t.Namespace == "Gameboard.Api.Data"
                && t.Name.EndsWith("Store")
                && t.IsClass
                && !t.IsAbstract
            )
        )
        {
            foreach (Type i in t.GetInterfaces())
                services.AddScoped(i, t);
            services.AddScoped(t);
        }

        return services;
    }

    // to support the integration test project
    public static DbContextOptionsBuilder UseGameboardPostgreSql(this DbContextOptionsBuilder dbContextOptionsBuilder, GameboardDataStoreConfig config)
        => dbContextOptionsBuilder
            .UseNpgsql(config.ConnectionString)
            .ConfigureDbContextOptions(config);

    private static DbContextOptionsBuilder ConfigureDbContextOptions(this DbContextOptionsBuilder builder, GameboardDataStoreConfig config)
        => builder
            .EnableSensitiveDataLogging(config.EnableSensitiveDataLogging)
            .UseLoggerFactory(LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(config.MinimumLogLevel);
            }));
}

