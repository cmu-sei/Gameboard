// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Gameboard.Api;
using Gameboard.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection;

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
                services.AddDbContextFactory<GameboardDbContext>(builder => builder.UseSqlServer(dbOptions.ConnectionString));
                break;
            case "postgresql":
                services.AddDbContextFactory<GameboardDbContext>(builder => builder.UseNpgsql(dbOptions.ConnectionString));
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

