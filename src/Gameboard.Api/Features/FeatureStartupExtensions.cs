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
using Gameboard.Api.Features.CubespaceScoreboard;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Player;
using Gameboard.Api.Features.UnityGames;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Gameboard.Api.Validators;
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
                .AddScoped<IAccessTokenProvider, HttpContextAccessTokenProvider>();

            // Auto-discover from EntityService pattern
            foreach (var t in Assembly
                .GetExecutingAssembly()
                .ExportedTypes
                .Where(t => (t.Namespace == "Gameboard.Api.Services" || t.BaseType == typeof(_Service))
                    && t.Name.EndsWith("Service")
                    && t.IsClass
                    && !t.IsAbstract
                )
            )
            {
                foreach (Type i in t.GetInterfaces())
                    services.AddScoped(i, t);
                services.AddScoped(t);
            }

            // TODO: Ben -> fix this
            services.AddHttpContextAccessor();
            services.AddUnboundServices(settings);

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

        // TODO: Ben -> fix this (still on my list, but for now at least segregating into a method)
        private static IServiceCollection AddUnboundServices(this IServiceCollection services, AppSettings settings)
            => services
                // singletons
                .AddSingleton<IAuthenticationService, AuthenticationService>()
                .AddSingleton<IJsonService, JsonService>(f => JsonService.WithGameboardSerializerOptions())
                .AddSingleton<ILockService, LockService>()
                .AddSingleton<INameService, NameService>()
                // global-style services
                .AddScoped<IAccessTokenProvider, HttpContextAccessTokenProvider>()
                .AddSingleton<CoreOptions>(_ => settings.Core)
                .AddSingleton<ApiKeyOptions>(_ => settings.ApiKey)
                .AddTransient<IGuidService, GuidService>()
                .AddTransient<IHashService, HashService>()
                .AddTransient<INowService, NowService>()
                .AddTransient<IRandomService, RandomService>()
                // feature services
                .AddScoped<IApiKeysService, ApiKeysService>()
                .AddScoped<IApiKeysStore, ApiKeysStore>()
                .AddScoped<Hub<IAppHubEvent>, AppHub>()
                .AddScoped<IChallengeStore, ChallengeStore>()
                .AddScoped<ICubespaceScoreboardService, CubespaceScoreboardService>()
                .AddScoped<IGamebrainService, GamebrainService>()
                .AddScoped<IGameEngineStore, GameEngineStore>()
                .AddScoped<IInternalHubBus, InternalHubBus>()
                .AddScoped<ITeamService, TeamService>()
                .AddScoped<IUnityGameService, UnityGameService>()
                .AddScoped<IUnityStore, UnityStore>();

        public static IMapperConfigurationExpression AddGameboardMaps(
            this IMapperConfigurationExpression cfg
        )
        {
            cfg.AddMaps(Assembly.GetExecutingAssembly());
            return cfg;
        }
    }
}
