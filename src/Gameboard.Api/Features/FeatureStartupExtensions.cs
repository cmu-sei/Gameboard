// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using AutoMapper;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceStartupExtensions
    {
        public static IServiceCollection AddGameboardServices(
            this IServiceCollection services
        )
        {
            services.AddSingleton<Gameboard.Api.Services.ConsoleActorMap>();

            // Auto-discover from EntityService pattern
            foreach (var t in Assembly
                .GetExecutingAssembly()
                .ExportedTypes
                .Where(t => t.Namespace == "Gameboard.Api.Services"
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

            foreach (var t in Assembly
                .GetExecutingAssembly()
                .ExportedTypes
                .Where(t => t.Namespace == "Gameboard.Api.Validators"
                    && t.Name.EndsWith("Validator")
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

        public static IMapperConfigurationExpression AddGameboardMaps(
            this IMapperConfigurationExpression cfg
        )
        {
            cfg.AddMaps(Assembly.GetExecutingAssembly());
            return cfg;
        }
    }
}
