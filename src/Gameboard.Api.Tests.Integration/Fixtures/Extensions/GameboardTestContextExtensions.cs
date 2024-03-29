using Gameboard.Api.Common;
using Gameboard.Api.Structure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal static class GameboardTestContextExtensions
{
    public static WebApplicationFactory<Program> BuildTestApplication(this GameboardTestContext testContext, Action<TestAuthenticationUser>? actingUserBuilder, Action<IServiceCollection>? configureTestServices = null)
    {
        var user = new TestAuthenticationUser();
        actingUserBuilder?.Invoke(user);

        return testContext
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // spoof authentication
                    services
                        .Configure<TestAuthenticationHandlerOptions>(options =>
                        {
                            var finalActor = user;

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

                    // and anything else the test wants
                    configureTestServices?.Invoke(services);
                });
            });
    }

    public static HttpClient CreateHttpClientWithActingUser(this GameboardTestContext testContext, Action<TestAuthenticationUser>? userBuilder = null)
    {
        return BuildTestApplication(testContext, userBuilder)
            .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public static HttpClient CreateHttpClientWithAuthRole(this GameboardTestContext testContext, UserRole role)
        => CreateHttpClientWithActingUser(testContext, u => u.Role = role);

    public static HttpClient CreateHttpClientWithGraderConfig(this GameboardTestContext testContext, double gradedScore)
        => CreateHttpClientWithGraderConfig(testContext, gradedScore, string.Empty);

    public static HttpClient CreateHttpClientWithGraderConfig(this GameboardTestContext testContext, double gradedScore, string graderKey)
    {
        var client = BuildTestApplication(testContext, u => { }, services =>
        {
            var testGradingResult = new TestGradingResultService(new TestGradingResultServiceConfiguration { GameStateBuilder = state => state.Challenge.Score = gradedScore });
            services.ReplaceService<ITestGradingResultService, TestGradingResultService>(testGradingResult);
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

