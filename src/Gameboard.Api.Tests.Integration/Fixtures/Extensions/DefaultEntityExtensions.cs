using Gameboard.Api.Common;
using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public static class GameboardTestContextDefaultEntityExtensions
{
    private static T BuildEntity<T>(T entity, Action<T>? builder = null) where T : class, Data.IEntity
    {
        builder?.Invoke(entity);
        return entity;
    }

    // eventually will replace these with registrations in the customization (like the integration test project does)
    private static string GenerateTestGuid() => Guid.NewGuid().ToString("n");

    public static TEntity Build<TEntity>(this IDataStateBuilder dataStateBuilder, IFixture fixture) where TEntity : class, IEntity
        => Build<TEntity>(dataStateBuilder, fixture, null);

    public static TEntity Build<TEntity>(this IDataStateBuilder dataStateBuilder, IFixture fixture, Action<TEntity>? entityBuilder) where TEntity : class, IEntity
    {
        var entity = fixture.Create<TEntity>();
        entityBuilder?.Invoke(entity);
        return entity;
    }

    public static void AddChallenge(this IDataStateBuilder dataStateBuilder, Action<Data.Challenge>? challengeBuilder = null)
        => dataStateBuilder.Add(BuildChallenge(dataStateBuilder, challengeBuilder));

    public static Data.Challenge BuildChallenge(this IDataStateBuilder dataStateBuilder, Action<Data.Challenge>? challengeBuilder = null)
        => BuildEntity
        (
            new Data.Challenge { Name = "Integration Test Challenge" },
            challengeBuilder
        );
}
