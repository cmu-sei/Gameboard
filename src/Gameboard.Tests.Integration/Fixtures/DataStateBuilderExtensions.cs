using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration.Fixtures;

internal static class DataStateBuilderExtensions
{
    public static IDataStateBuilder Add<T>(this IDataStateBuilder dataStateBuilder, Action<T>? entityBuilder = null) where T : class, IEntity
    {
        // var entity = Generators.Generate<T>();
        // entityBuilder?.Invoke(entity as IEntity);
        // dataStateBuilder.Add(entity);

        return dataStateBuilder;
    }
}