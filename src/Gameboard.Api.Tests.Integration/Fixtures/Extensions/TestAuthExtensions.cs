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

    public static IServiceCollection AddGbIntegrationTestAuth(this IServiceCollection services, UserRole role)
        => AddGbIntegrationTestAuth(services, u => u.Role = role);
}
