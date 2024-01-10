using Gameboard.Api.Common;
using Gameboard.Api.Structure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal static class GameboardTestContextExtensions
{
    private static WebApplicationFactory<Program> BuildBaseFactory(GameboardTestContext testContext, TestAuthenticationUser? actingUser = null)
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

        return BuildBaseFactory(testContext, user)
            .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public static HttpClient CreateHttpClientWithAuthRole(this GameboardTestContext testContext, UserRole role)
        => CreateHttpClientWithActingUser(testContext, u => u.Role = role);

    public static HttpClient CreateHttpClientWithGraderConfig(this GameboardTestContext testContext, double gradedScore)
        => CreateHttpClientWithGraderConfig(testContext, gradedScore, string.Empty);

    public static HttpClient CreateHttpClientWithGraderConfig(this GameboardTestContext testContext, double gradedScore, string graderKey)
    {
        var client = testContext
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    var testGradingResult = new TestGradingResultService(gradedScore, builder => builder.Challenge.Score = gradedScore);
                    services.ReplaceService<ITestGradingResultService, TestGradingResultService>(testGradingResult);
                });
            })
            .CreateClient();

        if (graderKey.IsNotEmpty())
        {
            client.DefaultRequestHeaders.Add(GraderKeyAuthentication.GraderKeyHeaderName, graderKey);
        }

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

