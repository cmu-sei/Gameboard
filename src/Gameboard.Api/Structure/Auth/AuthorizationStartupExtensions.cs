// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api;
using Gameboard.Api.Auth;
using Gameboard.Api.Structure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AuthorizationStartupExtensions
    {
        public static IServiceCollection AddConfiguredAuthorization(this IServiceCollection services)
        {
            // stops microsoft from doing weird things like renaming the "sub" claim to its own made-up value
            JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthorizationBuilder()
                .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes
                    (
                        JwtBearerDefaults.AuthenticationScheme,
                        ApiKeyAuthentication.AuthenticationScheme
                    ).Build())
                .AddPolicy(AppConstants.RegistrarPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        JwtBearerDefaults.AuthenticationScheme
                    )
                    .RequireClaim(AppConstants.RoleClaimName, UserRole.Registrar.ToString())
                    .Build()
)
                .AddPolicy(AppConstants.DesignerPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        JwtBearerDefaults.AuthenticationScheme
                    )
                    .RequireClaim(AppConstants.RoleClaimName, UserRole.Designer.ToString())
                    .Build()
)
                .AddPolicy(AppConstants.AdminPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        JwtBearerDefaults.AuthenticationScheme
                    )
                    .RequireClaim(AppConstants.RoleClaimName, UserRole.Admin.ToString())
                    .Build()
)
                .AddPolicy(AppConstants.TicketOnlyPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        TicketAuthentication.AuthenticationScheme
                    )
                    .Build()
)
                .AddPolicy(AppConstants.ConsolePolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        AppConstants.MksCookie
                    )
                    .Build()
)
                .AddPolicy(AppConstants.HubPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        TicketAuthentication.AuthenticationScheme,
                        AppConstants.MksCookie
                    )
                    .Build()
)
                .AddPolicy(AppConstants.GraderPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(
                        JwtBearerDefaults.AuthenticationScheme,
                        GraderKeyAuthentication.AuthenticationScheme
                    ).Build()
)
                .AddPolicy(AppConstants.ApiKeyAuthPolicy, new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddAuthenticationSchemes(ApiKeyAuthentication.AuthenticationScheme)
                    .Build());

            return services;
        }
    }

}
