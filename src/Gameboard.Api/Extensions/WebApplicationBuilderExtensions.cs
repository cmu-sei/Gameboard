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

        settings.Cache.SharedFolder = Path.Combine(
            builder.Environment.ContentRootPath,
            settings.Cache.SharedFolder ?? string.Empty
        );

        settings.Core.ImageFolder = Path.Combine(
            builder.Environment.ContentRootPath,
            settings.Core.ImageFolder ?? string.Empty
        );

        settings.Core.WebHostRoot = builder.Environment.ContentRootPath;

        if (settings.Core.ChallengeDocUrl.IsEmpty())
            settings.Core.ChallengeDocUrl = settings.PathBase;

        if (!settings.Core.ChallengeDocUrl?.EndsWith("/") ?? true)
            settings.Core.ChallengeDocUrl += "/";

        Directory.CreateDirectory(settings.Core.ImageFolder);

        settings.Core.TempDirectory = Path.Combine
        (
            builder.Environment.ContentRootPath,
            settings.Core.TempDirectory = "wwwroot/temp"
        );

        settings.Core.TemplatesDirectory = Path.Combine
        (
            builder.Environment.ContentRootPath,
            settings.Core.TemplatesDirectory ?? "wwwroot/templates"
        );

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

        // serilog config
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

        services
            .AddMvc()
            .AddGameboardJsonOptions();

        services
            .ConfigureForwarding(settings.Headers.Forwarding)
            .AddCors(opt => opt.AddPolicy(settings.Headers.Cors.Name, settings.Headers.Cors.Build()))
            .AddCache(() => settings.Cache);

        if (settings.OpenApi.Enabled)
            services.AddSwagger(settings.Oidc, settings.OpenApi);

        services.AddDataProtection()
            .SetApplicationName(AppConstants.DataProtectionPurpose)
            .PersistKeys(() => settings.Cache);

        services
            .AddSingleton(_ => settings.Core)
            .AddSingleton(_ => settings.Crucible)
            .AddGameboardData(builder.Environment, settings.Database)
            .AddGameboardMediatR()
            .AddGameboardServices(settings)
            .AddConfiguredHttpClients(settings.Core)
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
        services.AddConfiguredAuthentication(settings.Oidc, settings.ApiKey, builder.Environment);
        services.AddConfiguredAuthorization();
    }
}
