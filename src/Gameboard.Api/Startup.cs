// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceStack.Text;

namespace Gameboard.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostEnvironment env)
        {
            Configuration = configuration;

            Environment = env;

            Settings = Configuration.Get<AppSettings>() ?? new AppSettings();

            Settings.Cache.SharedFolder = Path.Combine(
                env.ContentRootPath,
                Settings.Cache.SharedFolder ?? ""
            );

            Settings.Core.ImageFolder = Path.Combine(
                env.ContentRootPath,
                Settings.Core.ImageFolder ?? ""
            );

            if (Settings.Core.ChallengeDocUrl.IsEmpty())
                Settings.Core.ChallengeDocUrl = Settings.PathBase;

            if (!Settings.Core.ChallengeDocUrl?.EndsWith("/") ?? true)
                Settings.Core.ChallengeDocUrl += "/";

            Directory.CreateDirectory(Settings.Core.ImageFolder);

            CsvConfig<Tuple<string, string>>.OmitHeaders = true;
            CsvConfig<Tuple<string, string, string>>.OmitHeaders = true;
            CsvConfig<ChallengeStatsExport>.OmitHeaders = true;
            CsvConfig<ChallengeDetailsExport>.OmitHeaders = true;

            if (env.IsDevelopment() || env.IsEnvironment("test"))
                Settings.Oidc.RequireHttpsMetadata = false;
        }

        public IHostEnvironment Environment { get; }
        public IConfiguration Configuration { get; }
        AppSettings Settings { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

                options.JsonSerializerOptions.Converters
                    .Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

                options.JsonSerializerOptions.Converters
                    .Add(new JsonDateTimeConverter());
            });

            services.ConfigureForwarding(Settings.Headers.Forwarding);

            services.AddCors(
                opt => opt.AddPolicy(
                    Settings.Headers.Cors.Name,
                    Settings.Headers.Cors.Build()
                )
            );

            if (Settings.OpenApi.Enabled)
                services.AddSwagger(Settings.Oidc, Settings.OpenApi);

            services.AddCache(() => Settings.Cache);

            services.AddDataProtection()
                .SetApplicationName(AppConstants.DataProtectionPurpose)
                .PersistKeys(() => Settings.Cache)
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
                .AddSingleton<CoreOptions>(_ => Settings.Core)
                .AddSingleton<INameService, NameService>()
                .AddGameboardData(Settings.Database.Provider, Settings.Database.ConnectionString)
                .AddGameboardServices()
                .AddConfiguredHttpClients(Settings.Core)
                .AddHostedService<JobService>()
                .AddDefaults(Settings.Defaults, Environment.ContentRootPath)
            ;

            services.AddSingleton<AutoMapper.IMapper>(
                new AutoMapper.MapperConfiguration(cfg =>
                {
                    cfg.AddGameboardMaps();
                }).CreateMapper()
            );

            // Configure Auth
            services.AddConfiguredAuthentication(Settings.Oidc);
            services.AddConfiguredAuthorization();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseJsonExceptions();

            if (!string.IsNullOrEmpty(Settings.PathBase))
                app.UsePathBase(Settings.PathBase);

            if (Settings.Headers.LogHeaders)
                app.UseHeaderInspection();

            if (!string.IsNullOrEmpty(Settings.Headers.Forwarding.TargetHeaders))
                app.UseForwardedHeaders();

            if (Settings.Headers.UseHsts)
                app.UseHsts();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors(Settings.Headers.Cors.Name);
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseFileProtection();
            app.UseStaticFiles();

            if (Settings.OpenApi.Enabled)
                app.UseConfiguredSwagger(Settings.OpenApi, Settings.Oidc.Audience, Settings.PathBase);

            app.UseEndpoints(ep =>
            {
                ep.MapHub<Hubs.AppHub>("/hub").RequireAuthorization();

                ep.MapControllers().RequireAuthorization();
            });
        }
    }
}
