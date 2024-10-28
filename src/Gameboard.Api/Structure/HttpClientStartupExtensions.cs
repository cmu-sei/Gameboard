// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using Alloy.Api.Client;
using Gameboard.Api;
using Polly;
using Polly.Extensions.Http;
using TopoMojo.Api.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StartupExtensions
    {
        public static IServiceCollection AddConfiguredHttpClients(
            this IServiceCollection services,
            CoreOptions config
        )
        {
            services.AddHttpClient<ITopoMojoApiClient, TopoMojoApiClient>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(config.GameEngineUrl);
                client.DefaultRequestHeaders.Add("x-api-key", config.GameEngineClientSecret);
                client.DefaultRequestHeaders.Add("x-api-client", config.GameEngineClientName);
                client.Timeout = TimeSpan.FromSeconds(300);
            })
            .AddPolicyHandler(
                HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(config.GameEngineMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            );

            services.AddHttpClient("topo", client =>
            {
                client.BaseAddress = new Uri(config.GameEngineUrl);
                client.DefaultRequestHeaders.Add("x-api-key", config.GameEngineClientSecret);
                client.DefaultRequestHeaders.Add("x-api-client", config.GameEngineClientName);
                client.Timeout = TimeSpan.FromSeconds(300);
            })
            .AddPolicyHandler
            (
                HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(config.GameEngineMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            );

            services.AddHttpClient("identity", client =>
            {
                // Workaround to avoid TaskCanceledException after several retries. TODO: find a better way to handle this.
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddPolicyHandler(
                HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(config.GameEngineMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            );

            services.AddHttpClient("alloy", client =>
            {
                // Workaround to avoid TaskCanceledException after several retries. TODO: find a better way to handle this.
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddHttpMessageHandler<AuthenticatingHandler>()
            .AddPolicyHandler(
                HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(config.GameEngineMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
            );

            services.AddScoped<IAlloyApiClient, AlloyApiClient>(p =>
            {
                var httpClientFactory = p.GetRequiredService<IHttpClientFactory>();
                var settings = p.GetRequiredService<CrucibleOptions>();

                var uri = new Uri(settings.ApiUrl);

                var httpClient = httpClientFactory.CreateClient("alloy");
                httpClient.BaseAddress = uri;

                return new AlloyApiClient(httpClient);
            });

            services.AddTransient<AuthenticatingHandler>();

            return services;
        }

    }
}
