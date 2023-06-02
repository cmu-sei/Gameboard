using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal static class GameboardTestContextExtensions
{
    public static GameboardTestContext<GameboardDbContextPostgreSQL> WithTestServices(this GameboardTestContext<GameboardDbContextPostgreSQL> testContext, Action<IServiceCollection> configureTestServices)
    {
        testContext._testServicesBuilder = configureTestServices;
        return testContext;
    }

    public static async Task WithDataState(this GameboardTestContext<GameboardDbContextPostgreSQL> context, Action<IDataStateBuilder> builderAction)
    {
        var dbContext = context.GetDbContext();

        var builderInstance = new DataStateBuilder<GameboardDbContextPostgreSQL>(dbContext);
        builderAction.Invoke(builderInstance);

        await dbContext.SaveChangesAsync();
    }
}
