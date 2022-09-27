// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
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

            services
                .AddHttpClient<ITopoMojoApiClient, TopoMojoApiClient>()
                    .ConfigureHttpClient(client =>
                    {
                        client.BaseAddress = new Uri(config.GameEngineUrl);
                        client.DefaultRequestHeaders.Add("x-api-key", config.GameEngineClientSecret);
                        client.DefaultRequestHeaders.Add("x-api-client", config.GameEngineClientName);
                    })
                    .AddPolicyHandler(
                        HttpPolicyExtensions.HandleTransientHttpError()
                        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                        .WaitAndRetryAsync(config.GameEngineMaxRetries, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    )
                ;
            services
                .AddHttpClient("Gamebrain", httpClient =>
                {
                    httpClient.BaseAddress = new Uri(config.GamebrainUrl);
                });
                

            return services;
        }

    }
}
