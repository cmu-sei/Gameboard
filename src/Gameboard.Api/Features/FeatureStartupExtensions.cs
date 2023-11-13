// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Gameboard.Api;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Features.Games.Validators;
using Gameboard.Api.Features.Reports;
using Gameboard.Api.Features.UnityGames;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceStartupExtensions
{
    public static IServiceCollection AddGameboardServices(this IServiceCollection services, AppSettings settings)
    {
        var jsonService = JsonService.WithGameboardSerializerOptions();

        // add special case services
        services
            .AddHttpContextAccessor()
            .AddScoped<IExternalGameHostAccessTokenProvider, HttpContextAccessTokenProvider>()
            .AddSingleton(_ => settings.Core)
            .AddSingleton(_ => settings.ApiKey)
            .AddSingleton(new BackgroundTaskContext())
            // explicitly singleton because it's a hosted service, so we want exactly one
            .AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>()
            .AddSingleton<IJsonService, JsonService>(f => jsonService)
            .AddSingleton(opts => JsonService.GetJsonSerializerOptions())
            .AddSingleton(jsonService)
            .AddConcretesFromNamespace("Gameboard.Api.Services")
            .AddConcretesFromNamespace("Gameboard.Api.Structure.Authorizers")
            .AddConcretesFromNamespace("Gameboard.Api.Structure.Validators")
            .AddScoped(typeof(IStore<>), typeof(Store<>))
            .AddScoped(typeof(UserIsPlayingGameValidator<>))
            .AddImplementationsOf<IGameboardRequestValidator<IReportQuery>>()
            // these aren't picked up right now because they implement multiple interfaces,
            // but allowing multiple-interface classes causes things like IReportQuery implementers to get snagged
            .AddScoped<IExternalSyncGameStartService, ExternalSyncGameStartService>()
            .AddScoped<IGameHubBus, GameHubBus>()
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
