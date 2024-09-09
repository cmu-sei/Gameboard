// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
                services.AddDbContext<GameboardDbContext, GameboardDbContextSqlServer>(builder => builder.UseSqlServer(dbOptions.ConnectionString), ServiceLifetime.Transient);
                break;
            case "postgresql":
                services.AddDbContext<GameboardDbContext, GameboardDbContextPostgreSQL>(builder => builder.UseNpgsql(dbOptions.ConnectionString), ServiceLifetime.Transient);
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
}

