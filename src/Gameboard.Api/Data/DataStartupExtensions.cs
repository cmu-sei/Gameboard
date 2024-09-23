// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Data;

public static class DataStartupExtensions
{
    public static IServiceCollection AddGameboardData
    (
        this IServiceCollection services,
        IWebHostEnvironment environment,
        DatabaseOptions dbOptions
    )
    {
        if (!environment.IsTest() && dbOptions.Provider.IsEmpty())
        {
            throw new Exception($"Can't start Gameboard without a storage provider.");
        }

        switch (dbOptions.Provider.ToLower())
        {
            case "sqlserver":
                services.AddDbContext<GameboardDbContext, GameboardDbContextSqlServer>((serviceProvider, options) => options
                    .UseSqlServer(dbOptions.ConnectionString)
                    .UseGameboardEfConfig(serviceProvider));
                break;
            case "postgresql":
                services.AddDbContext<GameboardDbContext, GameboardDbContextPostgreSQL>((serviceProvider, options) => options
                    .UseNpgsql(dbOptions.ConnectionString)
                    .UseGameboardEfConfig(serviceProvider));
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

    internal static DbContextOptionsBuilder UseGameboardEfConfig(this DbContextOptionsBuilder builder, IServiceProvider serviceProvider)
    {
        var env = serviceProvider.GetRequiredService<IHostEnvironment>();

        // we accommodate for the case that the environment is null (as it is during the creation of migrations)
        // by assuming that we don't need any detailed/sensitive logging - the environment must explicitly be set
        // to activate these behaviors
        if (env is null)
        {
            return builder.LogTo(Console.WriteLine, [DbLoggerCategory.Query.Name], LogLevel.Debug);
        }

        // warn us about queries that might benefit from query splitting
        // We usually don't mind this, because split queries only matter if tables are very large, but 
        // uncomment this to find issues if performance suffers or if warnings about split querying are 
        // shown
        // builder.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));

        if (env.IsDevOrTest())
        {
            builder.LogTo(Console.WriteLine, LogLevel.Information);

            if (env.IsDevelopment())
            {
                Console.WriteLine("Starting in dev environment. Enabling detailed/sensitive EF logging...");

                builder.LogTo(Console.WriteLine, [DbLoggerCategory.Query.Name], LogLevel.Information);
                builder
                    .EnableDetailedErrors()
                    .EnableSensitiveDataLogging();
            }
        }
        else
        {
            builder.LogTo(Console.WriteLine, LogLevel.Warning);
        }

        // when run at design-time, the service provider may be null (because there's no running app to create it)
        if (serviceProvider is not null)
            builder
                .AddInterceptors(new SlowCommandLogInterceptor(serviceProvider.GetRequiredService<ILoggerFactory>()));

        return builder;
    }
}

