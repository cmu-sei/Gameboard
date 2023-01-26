using System.Net.Http.Headers;
using Gameboard.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Gameboard.Tests.Integration.Fixtures;

internal static class GameboardTestContextExtensions
{
    private static WebApplicationFactory<Program> WithAuthentication(this GameboardTestContext<GameboardDbContextPostgreSQL> testContext, string userId = "integrationtester")
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

    // public static IDataStateBuilder WithDataState(this GameboardTestContext<GameboardDbContextPostgreSQL> context)
    // {
    //     return new DataStateBuilder<GameboardDbContextPostgreSQL>(context);
    // }

    public static async Task WithDataState(this GameboardTestContext<GameboardDbContextPostgreSQL> context, Action<IDataStateBuilder> builderAction)
    {
        var dbContext = context.GetDbContext();

        var builderInstance = new DataStateBuilder<GameboardDbContextPostgreSQL>(dbContext);
        builderAction.Invoke(builderInstance);

        await dbContext.SaveChangesAsync();
    }
}