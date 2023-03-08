// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Gameboard.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection
{

    public static class SwaggerStartupExtensions
    {
        public static IServiceCollection AddSwagger(
            this IServiceCollection services,
            OidcOptions oidc,
            OpenApiOptions openapi
        )
        {
            string xmlDoc = Assembly.GetExecutingAssembly().GetName().Name + ".xml";

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = openapi.ApiName,
                    Version = "v1",
                    Description = "API documentation and interaction",
                });

                options.EnableAnnotations();
                options.CustomSchemaIds(i => i.FullName);

                if (File.Exists(xmlDoc))
                    options.IncludeXmlComments(xmlDoc);

                if (!string.IsNullOrEmpty(oidc.Authority))
                {
                    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                    {

                        Type = SecuritySchemeType.OAuth2,

                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri(
                                    openapi.Client.AuthorizationUrl
                                    ?? $"{oidc.Authority}/connect/authorize"
                                ),
                                TokenUrl = new Uri(
                                    openapi.Client.TokenUrl
                                    ?? $"{oidc.Authority}/connect/token"
                                ),
                                Scopes = new Dictionary<string, string>
                                {
                                    { oidc.Audience, "User Access" }
                                }
                            }
                        },
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                            },
                            new[] { oidc.Audience }
                        }
                    });
                }
            });

            return services;
        }

        public static IApplicationBuilder UseConfiguredSwagger(
            this IApplicationBuilder app,
            OpenApiOptions openapi,
            string audience,
            string pathbase
        )
        {
            app.UseSwagger(cfg =>
            {
                cfg.RouteTemplate = "api/{documentName}/openapi.json";
            });

            app.UseSwaggerUI(cfg =>
            {
                cfg.RoutePrefix = "api";
                cfg.SwaggerEndpoint($"{pathbase}/api/v1/openapi.json", $"{openapi.ApiName} (v1)");
                cfg.OAuthClientId(openapi.Client.ClientId);
                cfg.OAuthAppName(openapi.Client.ClientName ?? openapi.Client.ClientId);
                cfg.OAuthScopes(audience);
                cfg.OAuthUsePkce();
            });

            return app;
        }
    }
}
