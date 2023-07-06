// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Gameboard.Api;
using Gameboard.Api.Common;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.ApiKeys;
using Gameboard.Api.Features.ChallengeBonuses;
using Gameboard.Api.Features.CubespaceScoreboard;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Features.Games.Start;
using Gameboard.Api.Features.Scores;
using Gameboard.Api.Features.Teams;
using Gameboard.Api.Features.UnityGames;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Gameboard.Api.Structure;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceStartupExtensions
{
    public static IServiceCollection AddGameboardServices(this IServiceCollection services, IWebHostEnvironment environment, AppSettings settings)
    {
        services
            .AddSingleton<ConsoleActorMap>()
            .AddHttpContextAccessor()
            .AddGameboardMediatR()
            .AddSingleton<IMapper>(
            new MapperConfiguration(cfg =>
            {
                cfg.AddGameboardMaps();
            }).CreateMapper());

        // don't add the job service during test - we don't want it to interfere with CI
        if (!environment.IsTest())
            services.AddHostedService<JobService>();

        // dev environment logging
        if (environment.IsDev())
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
        }

        // add feature services
        services
            .AddConcretesFromNamespace("Gameboard.Api.Structure.Authorizers")
            .AddConcretesFromNamespace("Gameboard.Api.Structure.Validators");

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

        foreach
        (var t in Assembly
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

        // TODO: Ben -> fix this
        services.AddUnboundServices(settings);

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
            .AddScoped<IExternalGameHostAccessTokenProvider, HttpContextAccessTokenProvider>()
            .AddScoped<IActingUserService, ActingUserService>()
            .AddSingleton<CoreOptions>(_ => settings.Core)
            .AddSingleton<ApiKeyOptions>(_ => settings.ApiKey)
            .AddTransient<IAppUrlService, AppUrlService>()
            .AddTransient<IFileUploadService, FileUploadService>()
            .AddTransient<IGuidService, GuidService>()
            .AddTransient<IHashService, HashService>()
            .AddTransient<INowService, NowService>()
            .AddTransient<IRandomService, RandomService>()
            .AddTransient<IVmUrlResolver, GameboardMksVmUrlResolver>()
            .AddTransient(typeof(IStore<>), typeof(Store<>))
            // feature services
            .AddScoped<IApiKeysService, ApiKeysService>()
            .AddScoped<IApiKeysStore, ApiKeysStore>()
            .AddScoped<IGameboardValidator<ConfigureGameAutoBonusesCommand>, ConfigureGameAutoBonusesValidator>()
            .AddScoped<IStore<ManualChallengeBonus>, ManualChallengeBonusStore>()
            .AddScoped<Hub<IAppHubEvent>, AppHub>()
            .AddScoped<IChallengeStore, ChallengeStore>()
            .AddScoped<IChallengeBonusStore, ChallengeBonusStore>()
            .AddScoped<ICubespaceScoreboardService, CubespaceScoreboardService>()
            .AddScoped<DeleteGameAutoBonusesConfigValidator>()
            .AddScoped<IExternalSyncGameStartService, ExternalSyncGameStartService>()
            .AddScoped<IGamebrainService, GamebrainService>()
            .AddScoped<IGameEngineStore, GameEngineStore>()
            .AddScoped<IGameStartService, GameStartService>()
            .AddScoped<IGameHubBus, GameHubBus>()
            .AddScoped<IInternalHubBus, InternalHubBus>()
            .AddScoped<IGameHubBus, GameHubBus>()
            .AddScoped<IScoringService, ScoringService>()
            .AddScoped<ISyncStartGameService, SyncStartGameService>()
            .AddScoped<ITeamService, TeamService>()
            .AddScoped<IUnityGameService, UnityGameService>()
            .AddScoped<IUnityStore, UnityStore>()
            .AddScoped<IValidatorServiceFactory, ValidatorServiceFactory>();

    public static IMapperConfigurationExpression AddGameboardMaps(this IMapperConfigurationExpression cfg)
    {
        cfg.AddMaps(Assembly.GetExecutingAssembly());
        return cfg;
    }
}
