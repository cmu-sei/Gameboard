// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Data;
using Microsoft.AspNetCore.Authorization;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public static class TestAuthExtensions
{
    public static IServiceCollection AddGbIntegrationTestAuth(this IServiceCollection services, TestAuthenticationUser? actingUser = null)
    {
        // spoof authentication
        services
            .Configure<TestAuthenticationHandlerOptions>(options =>
            {
                var finalActor = actingUser ?? new TestAuthenticationUser { };

                options.DefaultUserId = finalActor.Id;
                options.Actor = finalActor;
            })
            .AddAuthentication(TestAuthenticationHandler.AuthenticationSchemeName)
            .AddScheme<TestAuthenticationHandlerOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationSchemeName, options => { });

        // and authorization
        services.AddAuthorization(config =>
        {
            config.DefaultPolicy = new AuthorizationPolicyBuilder(config.DefaultPolicy)
                .AddAuthenticationSchemes(TestAuthenticationHandler.AuthenticationSchemeName)
                .Build();
        });

        return services;
    }

    public static IServiceCollection AddGbIntegrationTestAuth(this IServiceCollection services, Action<TestAuthenticationUser> userBuilder)
    {
        var user = new TestAuthenticationUser();
        userBuilder.Invoke(user);
        return AddGbIntegrationTestAuth(services, user);
    }

    public static IServiceCollection AddGbIntegrationTestAuth(this IServiceCollection services, UserRoleKey role)
        => AddGbIntegrationTestAuth(services, u => u.Role = role);
}
