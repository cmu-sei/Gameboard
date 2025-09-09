using System;
using System.IO;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ServiceStack.Text;

namespace Gameboard.Api.Extensions;

internal static class WebApplicationBuilderExtensions
{
    public static AppSettings BuildAppSettings(this WebApplicationBuilder builder)
    {
        var settings = builder.Configuration.Get<AppSettings>() ?? new AppSettings();

        // If in dev or integration tests, go easy on HTTPS
        if (builder.Environment.IsDevOrTest())
        {
            settings.Oidc.RequireHttpsMetadata = false;
        }

        // apply defaults
        settings.Cache.SharedFolder = Path.Combine(builder.Environment.ContentRootPath, settings.Cache.SharedFolder ?? string.Empty);
        settings.Core.ImageFolder = Path.Combine(builder.Environment.ContentRootPath, settings.Core.ImageFolder ?? string.Empty);
        settings.Core.TempDirectory = Path.Combine(builder.Environment.ContentRootPath, settings.Core.TempDirectory = "wwwroot/temp");
        settings.Core.TemplatesDirectory = Path.Combine(builder.Environment.ContentRootPath, settings.Core.TemplatesDirectory ?? "wwwroot/templates");
        settings.Core.WebHostRoot = builder.Environment.ContentRootPath;

        if (settings.Core.ChallengeDocUrl.IsEmpty())
        {
            settings.Core.ChallengeDocUrl = settings.PathBase;
        }

        if (!settings.Core.ChallengeDocUrl?.EndsWith("/") ?? true)
        {
            settings.Core.ChallengeDocUrl += "/";
        }

        Directory.CreateDirectory(settings.Core.ImageFolder);

        // I think this config is for old reports, we may be able to pull
        CsvConfig<Tuple<string, string>>.OmitHeaders = true;
        CsvConfig<Tuple<string, string, string>>.OmitHeaders = true;
        CsvConfig<ChallengeStatsExport>.OmitHeaders = true;
        CsvConfig<ChallengeDetailsExport>.OmitHeaders = true;

        return settings;
    }

    public static void ConfigureServices(this WebApplicationBuilder builder, AppSettings settings, Microsoft.Extensions.Logging.ILogger logger)
    {
        var services = builder.Services;

        services
            .AddMvc()
            .AddGameboardJsonOptions();

        services
            .ConfigureForwarding(settings.Headers.Forwarding)
            .AddCors(opt => opt.AddPolicy(settings.Headers.Cors.Name, settings.Headers.Cors.Build()))
            .AddCache(() => settings.Cache);

        if (settings.OpenApi.Enabled)
        {
            services.AddSwagger(settings.Oidc, settings.OpenApi);
        }

        services
            .AddDataProtection()
            .SetApplicationName(AppConstants.DataProtectionPurpose)
            .PersistKeys(() => settings.Cache);

        builder.AddGameboardSerilog(settings);

        services
            .AddSingleton(_ => settings.Core)
            .AddSingleton(_ => settings.Crucible)
            .AddSingleton(_ => settings.Oidc)
            .AddGameboardData(builder.Environment, settings.Database)
            .AddGameboardMediatR()
            .AddGameboardServices(settings)
            .AddConfiguredHttpClients(settings, logger)
            .AddDefaults(settings.Defaults, builder.Environment.ContentRootPath);

        // HOSTED SERVICES
        // don't add these during test - we don't want them interfere with CI
        if (!builder.Environment.IsTest())
            services
                .AddHostedService<BackgroundAsyncTaskRunner>()
                .AddHostedService<JobService>();

        services.AddSingleton(new MapperConfiguration(cfg => cfg.AddGameboardMaps()).CreateMapper());

        // configuring SignalR involves acting on the builder as well as its services
        builder.AddGameboardSignalRServices();

        // Configure Auth
        services.AddConfiguredAuthentication(settings.Oidc, settings.ApiKey, builder.Environment, logger);
        services.AddConfiguredAuthorization();
    }

    private static WebApplicationBuilder AddGameboardSerilog(this WebApplicationBuilder builder, AppSettings settings)
    {
        // SERILOG CONFIG
        // Gameboard uses Serilog, which is awesome, because Serilog is awesome. By default, it just
        // writes to the console sink, which you can ingest with any log ingester (and is useful if you
        // choose to monitor the output of its pod in a K8s-style scenario). But if you want richer logging,
        //  you can add a Seq instance using its configuration so you get nice metadata like the userID
        // and name for API requests. Want to use a non-Seq sink? We get it. PR us and let's talk about it.
        builder.Host.UseSerilog();

        Serilog.Debugging.SelfLog.Enable(Console.Error);
        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(settings.Logging.MinimumLogLevel)
            .WriteTo.Console(theme: AnsiConsoleTheme.Code);

        // normally, you'd do this in an appsettings.json and just rely on built-in config
        // assembly stuff, but we distribute with a helm chart and a weird conf format, so
        // we need to manually set up the log levels
        foreach (var logNamespace in settings.Logging.NamespacesErrorLevel)
        {
            loggerConfiguration = loggerConfiguration.MinimumLevel.Override(logNamespace, LogEventLevel.Error);
        }

        foreach (var logNamespace in settings.Logging.NamespacesFatalLevel)
        {
            loggerConfiguration = loggerConfiguration.MinimumLevel.Override(logNamespace, LogEventLevel.Fatal);
        }

        foreach (var logNamespace in settings.Logging.NamespacesInfoLevel)
        {
            loggerConfiguration = loggerConfiguration.MinimumLevel.Override(logNamespace, LogEventLevel.Information);
        }

        foreach (var logNamespace in settings.Logging.NamespacesWarningLevel)
        {
            loggerConfiguration = loggerConfiguration.MinimumLevel.Override(logNamespace, LogEventLevel.Warning);
        }

        // set up sinks on demand
        if (settings.Logging.SeqInstanceUrl.IsNotEmpty())
        {
            loggerConfiguration = loggerConfiguration.WriteTo.Seq(settings.Logging.SeqInstanceUrl, apiKey: settings.Logging.SeqInstanceApiKey);
        }

        // weirdly, this really does appear to be the way to replace the default logger with Serilog 🤷
        Log.Logger = loggerConfiguration.CreateLogger();

        return builder;
    }
}
