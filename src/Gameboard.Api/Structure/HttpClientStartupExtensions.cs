// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using Alloy.Api.Client;
using Gameboard.Api;
using Gameboard.Api.Structure.Auth.Crucible;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using TopoMojo.Api.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StartupExtensions
    {
        public static IServiceCollection AddConfiguredHttpClients(this IServiceCollection services, AppSettings settings, ILogger logger)
        {
            logger.LogInformation("Configuring HTTP clients...");

            if (settings.Core.GameEngineClientName.IsNotEmpty() && settings.Core.GameEngineClientSecret.IsNotEmpty())
            {
                logger.LogInformation("Configuring game engine access via LEGACY API key...");

                // if API credentials are present, attach them to the request.
                // NOTE: API credentials are a legacy implementation and not preferred. Instead, create a client
                // using your identity provider and configure Gameboard with its client ID and secret using the
                // GameEngine__ClientId and GameEngine__ClientSecret settings.
                services
                    .AddHttpClient<ITopoMojoApiClient, TopoMojoApiClient>()
                    .ConfigureHttpClient(client =>
                    {
                        client.BaseAddress = new Uri(settings.Core.GameEngineUrl);
                        client.Timeout = TimeSpan.FromSeconds(300);
                        client.DefaultRequestHeaders.Add("x-api-client", settings.Core.GameEngineClientName);
                        client.DefaultRequestHeaders.Add("x-api-key", settings.Core.GameEngineClientSecret);
                    })
                    .AddPolicyHandler
                    (
                        HttpPolicyExtensions
                            .HandleTransientHttpError()
                            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                            .WaitAndRetryAsync(settings.Core.GameEngineMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    );
            }
            else if (settings.GameEngine.ClientId.IsNotEmpty() && settings.GameEngine.ClientSecret.IsNotEmpty())
            {
                logger.LogInformation("Configuring game engine access via client credentials...");
                services.AddCrucibleServiceAccountAuthentication(new CrucibleServiceAccountAuthenticationConfig
                {
                    OidcAudience = settings.Oidc.Audience,
                    OidcAuthority = settings.Oidc.Authority,
                    ClientId = settings.GameEngine.ClientId,
                    ClientSecret = settings.GameEngine.ClientSecret
                });

                services
                    .AddHttpClient<ITopoMojoApiClient, TopoMojoApiClient>()
                    .ConfigureCrucibleServiceAccountClient(new Uri(settings.Core.GameEngineUrl), settings.Core.GameEngineMaxRetries);
            }
            else
            {
                throw new Exception("Gameboard's HTTP access to the game engine has not been configured. Set the GameEngine__ClientId and GameEngine__ClientSecret options to enable client credentials-based access, or (legacy) set Core__GameEngineClientName and Core__GameEngineClientSecret for API-key based access.");
            }

            services
                .AddHttpClient("identity", client =>
                {
                    // Workaround to avoid TaskCanceledException after several retries. TODO: find a better way to handle this.
                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
                .AddPolicyHandler
                (
                    HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                        .WaitAndRetryAsync(settings.Core.GameEngineMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                );

            services
                .AddHttpClient("alloy", client =>
                {
                    // Workaround to avoid TaskCanceledException after several retries. TODO: find a better way to handle this.
                    client.Timeout = Timeout.InfiniteTimeSpan;
                })
                .AddHttpMessageHandler<AuthenticatingHandler>()
                .AddPolicyHandler
                (
                    HttpPolicyExtensions
                        .HandleTransientHttpError()
                        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                        .WaitAndRetryAsync(settings.Core.GameEngineMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                );

            services
                .AddScoped<IAlloyApiClient, AlloyApiClient>(p =>
                {
                    var httpClientFactory = p.GetRequiredService<IHttpClientFactory>();
                    var settings = p.GetRequiredService<CrucibleOptions>();

                    var httpClient = httpClientFactory.CreateClient("alloy");
                    httpClient.BaseAddress = new Uri(settings.ApiUrl);

                    return new AlloyApiClient(httpClient);
                });

            services.AddTransient<AuthenticatingHandler>();

            return services;
        }
    }
}
