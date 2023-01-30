// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Gameboard.Api.Features.CubespaceScoreboard;
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
        public static IServiceCollection AddGameboardServices(this IServiceCollection services)
        {
            services.AddSingleton<ConsoleActorMap>();

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
            services.AddScoped<IAccessTokenProvider, HttpContextAccessTokenProvider>();
            services.AddScoped<Hub<IAppHubEvent>, AppHub>();
            services.AddScoped<IInternalHubBus, InternalHubBus>();
            services.AddScoped<ITeamService, TeamService>();
            services.AddScoped<IUnityGameService, UnityGameService>();
            services.AddScoped<IUnityStore, UnityStore>();
            services.AddScoped<ICubespaceScoreboardService, CubespaceScoreboardService>();
            services.AddScoped<IGamebrainService, GamebrainService>();
            services.AddTransient<IGuidService, GuidService>();

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

        public static IMapperConfigurationExpression AddGameboardMaps(
            this IMapperConfigurationExpression cfg
        )
        {
            cfg.AddMaps(Assembly.GetExecutingAssembly());
            return cfg;
        }
    }
}
