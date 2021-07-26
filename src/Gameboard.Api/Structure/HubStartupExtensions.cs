// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.SignalR;
using Gameboard.Api.Hubs;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HubStartupExtensions
    {
        public static IServiceCollection AddSignalRHub(
            this IServiceCollection services
        ) {
            services.AddSignalR(options => {});

            services
                // .AddSingleton<HubCache>()
                .AddSingleton<IUserIdProvider, SubjectProvider>()
            ;

            return services;
        }

    }

}
