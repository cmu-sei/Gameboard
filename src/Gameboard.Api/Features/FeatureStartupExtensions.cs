// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Gameboard.Api;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ApiKeys;
using Gameboard.Api.Features.ChallengeBonuses;
using Gameboard.Api.Features.CubespaceScoreboard;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.UnityGames;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceStartupExtensions
    {
        public static IServiceCollection AddGameboardServices(this IServiceCollection services, AppSettings settings)
        {
            // add special case services
            services
                .AddSingleton<ConsoleActorMap>()
                .AddHttpContextAccessor()
                .AddScoped<IAccessTokenProvider, HttpContextAccessTokenProvider>()
                .AddSingleton<CoreOptions>(_ => settings.Core)
                .AddSingleton<ApiKeyOptions>(_ => settings.ApiKey)
                .AddSingleton<IJsonService, JsonService>(f => JsonService.WithGameboardSerializerOptions())
                .AddConcretesFromNamespace("Gameboard.Api.Services")
                .AddConcretesFromNamespace("Gameboard.Api.Structure.Authorizers")
                .AddConcretesFromNamespace("Gameboard.Api.Structure.Validators")
                .AddScoped(typeof(IStore<>), typeof(Store<>))
                // so close to fixing this, but it's a very special snowflake of a binding
                .AddScoped<IUnityStore, UnityStore>()
                .AddInterfacesWithSingleImplementations();

            foreach (var t in Assembly
                .GetExecutingAssembly()
                .ExportedTypes
                .Where(t =>
                    t.GetInterface(nameof(IModelValidator)) != null
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

        public static IMapperConfigurationExpression AddGameboardMaps(this IMapperConfigurationExpression cfg)
        {
            cfg.AddMaps(Assembly.GetExecutingAssembly());
            return cfg;
        }
    }
}
