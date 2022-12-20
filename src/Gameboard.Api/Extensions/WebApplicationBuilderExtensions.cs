using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Text;

namespace Gameboard.Api.Extensions;

internal static class WebApplicationBuilderExtensions
{
    // exposed internally to support integration testing
    public static Action<JsonOptions> ConfigureJsonOptions { get; } = (options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.JsonSerializerOptions.Converters.Add(new JsonDateTimeConverter());
    });

    public static AppSettings BuildAppSettings(this WebApplicationBuilder builder)
    {
        var settings = builder.Configuration.Get<AppSettings>() ?? new AppSettings();

        settings.Cache.SharedFolder = Path.Combine(
            builder.Environment.ContentRootPath,
            settings.Cache.SharedFolder ?? ""
        );

        settings.Core.ImageFolder = Path.Combine(
            builder.Environment.ContentRootPath,
            settings.Core.ImageFolder ?? ""
        );

        if (settings.Core.ChallengeDocUrl.IsEmpty())
            settings.Core.ChallengeDocUrl = settings.PathBase;

        if (!settings.Core.ChallengeDocUrl?.EndsWith("/") ?? true)
            settings.Core.ChallengeDocUrl += "/";

        Directory.CreateDirectory(settings.Core.ImageFolder);

        CsvConfig<Tuple<string, string>>.OmitHeaders = true;
        CsvConfig<Tuple<string, string, string>>.OmitHeaders = true;
        CsvConfig<ChallengeStatsExport>.OmitHeaders = true;
        CsvConfig<ChallengeDetailsExport>.OmitHeaders = true;

        if (builder.Environment.IsDevOrTest())
            settings.Oidc.RequireHttpsMetadata = false;

        return settings;
    }

    public static void ConfigureServices(this WebApplicationBuilder builder, AppSettings settings)
    {
        var services = builder.Services;

        services.AddMvc()
            .AddJsonOptions(ConfigureJsonOptions);

        services.ConfigureForwarding(settings.Headers.Forwarding);

        services.AddCors(
            opt => opt.AddPolicy(
                settings.Headers.Cors.Name,
                settings.Headers.Cors.Build()
            )
        );

        if (settings.OpenApi.Enabled)
            services.AddSwagger(settings.Oidc, settings.OpenApi);

        services.AddCache(() => settings.Cache);

        services.AddDataProtection()
            .SetApplicationName(AppConstants.DataProtectionPurpose)
            .PersistKeys(() => settings.Cache)
        ;

        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(
                    JsonNamingPolicy.CamelCase
                ));
            })
        ;
        services.AddSignalRHub();

        services
            .AddSingleton<CoreOptions>(_ => settings.Core)
            .AddSingleton<INameService, NameService>()
            .AddGameboardData(settings.Database.Provider, settings.Database.ConnectionString)
            .AddGameboardServices()
            .AddConfiguredHttpClients(settings.Core)
            .AddHostedService<JobService>()
            .AddDefaults(settings.Defaults, builder.Environment.ContentRootPath)
        ;

        services.AddSingleton<AutoMapper.IMapper>(
            new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddGameboardMaps();
            }).CreateMapper()
        );

        // Configure Auth
        services.AddConfiguredAuthentication(settings.Oidc);
        services.AddConfiguredAuthorization();
    }
}
