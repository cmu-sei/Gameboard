// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Gameboard.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DataStartupExtensions
    {
        public static IServiceCollection AddGameboardData
        (
            this IServiceCollection services,
            IWebHostEnvironment env,
            ILogger logger,
            string provider,
            string connstr
        )
        {
            switch (provider.ToLower())
            {
                case "sqlserver":
                    services.AddDbContext<GameboardDbContext, GameboardDbContextSqlServer>(builder =>
                        builder
                            .WithGameboardOptions(env)
                            .UseSqlServer(connstr)
                    );
                    break;

                case "postgresql":
                    services.AddDbContext<GameboardDbContext, GameboardDbContextPostgreSQL>(builder =>
                        builder
                            .WithGameboardOptions(env)
                            .UseNpgsql(connstr)
                    );

                    break;
                default:
                    services.AddDbContext<GameboardDbContext, GameboardDbContextInMemory>(builder =>
                        builder
                            .WithGameboardOptions(env)
                            .UseInMemoryDatabase("Gameboard_Db")
                    );
                    break;
            }

            // load stores by reflection
            foreach (var t in Assembly
                .GetExecutingAssembly()
                .ExportedTypes
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
}
