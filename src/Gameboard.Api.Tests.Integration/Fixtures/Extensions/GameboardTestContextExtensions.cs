using Gameboard.Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal static class GameboardTestContextExtensions
{
    public static GameboardTestContext<GameboardDbContextPostgreSQL> WithTestServices(this GameboardTestContext<GameboardDbContextPostgreSQL> testContext, Action<IServiceCollection> configureTestServices)
    {
        testContext = testContext.WithWebHostBuilder(builder => builder.ConfigureTestServices(services => configureTestServices(services)));
        return testContext;
    }

    public static HttpClient CreateGbApiClient(this GameboardTestContext<GameboardDbContextPostgreSQL> testContext)
    {
        return testContext.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public static async Task WithDataState(this GameboardTestContext<GameboardDbContextPostgreSQL> context, Action<IDataStateBuilder> builderAction)
    {
        var dbContext = context.GetDbContext();

        var builderInstance = new DataStateBuilder<GameboardDbContextPostgreSQL>(dbContext);
        builderAction.Invoke(builderInstance);

        await dbContext.SaveChangesAsync();
    }
}
