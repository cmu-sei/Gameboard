// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api;
using Gameboard.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AuthorizationStartupExtensions
    {
        public static IServiceCollection AddConfiguredAuthorization(
            this IServiceCollection services
        )
        {
            services.AddAuthorization(_ =>
            {
                _.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes
                    (
                        JwtBearerDefaults.AuthenticationScheme,
                        ApiKeyAuthentication.AuthenticationScheme
                    ).Build();

                _.AddPolicy(AppConstants.RegistrarPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        JwtBearerDefaults.AuthenticationScheme
                    )
                    .RequireClaim(AppConstants.RoleClaimName, UserRole.Registrar.ToString())
                    .Build()
                );

                _.AddPolicy(AppConstants.DesignerPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        JwtBearerDefaults.AuthenticationScheme
                    )
                    .RequireClaim(AppConstants.RoleClaimName, UserRole.Designer.ToString())
                    .Build()
                );

                _.AddPolicy(AppConstants.AdminPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        JwtBearerDefaults.AuthenticationScheme
                    )
                    .RequireClaim(AppConstants.RoleClaimName, UserRole.Admin.ToString())
                    .Build()
                );

                _.AddPolicy(AppConstants.TicketOnlyPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        TicketAuthentication.AuthenticationScheme
                    )
                    .Build()
                );

                _.AddPolicy(AppConstants.ConsolePolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        AppConstants.MksCookie
                    )
                    .Build()
                );

                _.AddPolicy(AppConstants.HubPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        TicketAuthentication.AuthenticationScheme,
                        AppConstants.MksCookie
                    )
                    .Build()
                );

                _.AddPolicy(AppConstants.GraderPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        JwtBearerDefaults.AuthenticationScheme,
                        ApiKeyAuthentication.AuthenticationScheme
                    ).Build()
                );

                _.AddPolicy(AppConstants.ApiKeyAuthPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(ApiKeyAuthentication.AuthenticationScheme)
                    .Build());
            });

            return services;
        }
    }

}
