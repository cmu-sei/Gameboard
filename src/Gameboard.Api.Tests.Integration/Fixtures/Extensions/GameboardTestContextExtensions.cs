using System.Net.Http.Headers;
using Gameboard.Api.Structure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal static class GameboardTestContextExtensions
{
    private static WebApplicationFactory<Program> GetWebApplicationFactory(this GameboardTestContext testContext)
    {
        return testContext
            .WithWebHostBuilder(builder =>
            {
                // builder.ConfigureTestServices(services =>
                // {
                //     // fake graderkey auth
                //     // services
                //     //     .Configure<TestGraderKeyAuthenticationHandlerOptions>(options =>
                //     //     {
                //     //         options.ChallengeGraderKey = graderKey;
                //     //     })
                //     //     .AddAuthentication(TestGraderKeyAuthenticationHandler.AuthenticationSchemeName)
                //     //     .AddScheme<TestGraderKeyAuthenticationHandlerOptions, TestGraderKeyAuthenticationHandler>(TestGraderKeyAuthenticationHandler.AuthenticationSchemeName, options => { });
                //     services.Configure<GraderKEyAuth                    
                // });
            });
    }

    private static WebApplicationFactory<Program> BuildUserAuthentication(this GameboardTestContext testContext, TestAuthenticationUser? actingUser = null)
    {
        return testContext
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
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
                });
            });
    }

    public static HttpClient CreateHttpClientWithActingUser(this GameboardTestContext testContext, Action<TestAuthenticationUser>? userBuilder = null)
    {
        var user = new TestAuthenticationUser();
        userBuilder?.Invoke(user);

        return BuildUserAuthentication(testContext, user)
            .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public static HttpClient CreateHttpClientWithActingUser(this GameboardTestContext testContext, Data.User user)
    {
        var client = testContext
            .CreateHttpClientWithActingUser(u =>
            {
                u.Id = user.Id;
                u.Name = user.Name;
                u.Role = user.Role;
            });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthenticationHandler.AuthenticationSchemeName);
        return client;
    }

    public static HttpClient CreateHttpClientWithAuthRole(this GameboardTestContext testContext, UserRole role)
        => CreateHttpClientWithActingUser(testContext, u => u.Role = role);

    public static HttpClient CreateHttpClientWithGraderKey(this GameboardTestContext testContext, string graderKey)
    {
        var client = testContext
            .GetWebApplicationFactory()
            .CreateDefaultClient();

        client.DefaultRequestHeaders.Add(GraderKeyAuthentication.GraderKeyHeaderName, graderKey);
        return client;
    }

    public static async Task WithDataState(this GameboardTestContext context, Action<IDataStateBuilder> builderAction)
    {
        var dbContext = context.GetDbContext();

        var builderInstance = new DataStateBuilder(dbContext);
        builderAction.Invoke(builderInstance);

        await dbContext.SaveChangesAsync();
    }
}
