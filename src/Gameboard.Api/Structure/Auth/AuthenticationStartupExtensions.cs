// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using Gameboard.Api;
using Gameboard.Api.Auth;
using Gameboard.Api.Structure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AuthenticationStartupExtensions
    {
        public static IServiceCollection AddConfiguredAuthentication
        (
            this IServiceCollection services,
            OidcOptions oidcOptions,
            ApiKeyOptions apiKeyOptions,
            IWebHostEnvironment environment
        )
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            if (environment.IsDevelopment())
            {
                IdentityModelEventSource.ShowPII = true;
            }

            _ = services
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
                        HttpOnly = true,
                        SecurePolicy = CookieSecurePolicy.SameAsRequest,
                        SameSite = SameSiteMode.Strict
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
                .AddGraderKeyAuthentication(GraderKeyAuthentication.AuthenticationScheme, opt => { })
                .AddApiKeyAuthentication(ApiKeyAuthentication.AuthenticationScheme, opt =>
                {
                    opt.BytesOfRandomness = apiKeyOptions.BytesOfRandomness;
                    opt.RandomCharactersLength = apiKeyOptions.RandomCharactersLength;
                })
                .AddTicketAuthentication(TicketAuthentication.AuthenticationScheme, opt => { })
            ;

            return services;
        }
    }
}
