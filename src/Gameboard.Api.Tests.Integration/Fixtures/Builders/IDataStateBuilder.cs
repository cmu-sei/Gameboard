namespace Gameboard.Api.Tests.Integration.Fixtures;

public interface IDataStateBuilder
{
    IDataStateBuilder Add<TEntity>(TEntity entity, Action<TEntity>? entityBuilder = null) where TEntity : class, Data.IEntity;
    IDataStateBuilder AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, Data.IEntity;
}
