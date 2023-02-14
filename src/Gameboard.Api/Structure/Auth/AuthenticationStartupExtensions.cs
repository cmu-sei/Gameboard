// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using Gameboard.Api;
using Gameboard.Api.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AuthenticationStartupExtensions
    {
        public static IServiceCollection AddConfiguredAuthentication(
            this IServiceCollection services,
            OidcOptions oidcOptions,
            ApiKeyOptions apiKeyOptions
        )
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services
                .AddScoped<IClaimsTransformation, UserClaimTransformation>()
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(jwt =>
                {
                    jwt.Audience = oidcOptions.Audience;
                    jwt.Authority = oidcOptions.Authority;
                    jwt.RequireHttpsMetadata = oidcOptions.RequireHttpsMetadata;

                    jwt.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = AppConstants.NameClaimName,
                        RoleClaimType = AppConstants.RoleClaimName
                    };

                    jwt.SaveToken = true;
                })
                .AddCookie(AppConstants.MksCookie, opt =>
                {
                    opt.ExpireTimeSpan = new System.TimeSpan(0, oidcOptions.MksCookieMinutes, 0);
                    opt.Cookie = new CookieBuilder
                    {
                        Name = AppConstants.MksCookie,
                    };
                    opt.Events.OnRedirectToAccessDenied = ctx =>
                    {
                        ctx.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return System.Threading.Tasks.Task.CompletedTask;
                    };
                    opt.Events.OnRedirectToLogin = ctx =>
                    {
                        ctx.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return System.Threading.Tasks.Task.CompletedTask;
                    };
                })
                .AddApiKeyAuthentication(ApiKeyAuthentication.AuthenticationScheme, opt =>
                {
                    opt.BytesOfRandomness = apiKeyOptions.BytesOfRandomness;
                    opt.IsEnabled = apiKeyOptions.IsEnabled;
                    opt.RandomCharactersLength = apiKeyOptions.RandomCharactersLength;
                })
                .AddTicketAuthentication(TicketAuthentication.AuthenticationScheme, opt => new TicketAuthenticationOptions())
            ;

            return services;
        }
    }
}
