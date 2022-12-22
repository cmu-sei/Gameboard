using System.Net.Http.Headers;
using Gameboard.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Gameboard.Tests.Integration.Extensions;

internal static class GameboardTestContextExtensions
{
    public static WebApplicationFactory<Program> WithAuthentication(this GameboardTestContext<GameboardDbContextPostgreSQL> testContext, string userId = "integrationtester")
    {
        return testContext
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // spoof authentication
                    services
                        .Configure<TestAuthenticationHandlerOptions>(options => options.DefaultUserId = userId)
                        .AddAuthentication(TestAuthenticationHandler.AuthenticationSchemeName)
                        .AddScheme<TestAuthenticationHandlerOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationSchemeName, options => { });

                    // and authorization
                    services.AddAuthorization(config =>
                    {
                        config.DefaultPolicy = new AuthorizationPolicyBuilder(config.DefaultPolicy)
                            .AddAuthenticationSchemes(TestAuthenticationHandler.AuthenticationSchemeName)
                            .Build();
                    });
                });
            });
    }

    public static HttpClient CreateHttpClientWithAuth(this GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        var client = testContext
            .WithAuthentication()
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthenticationHandler.AuthenticationSchemeName);
        return client;
    }
}