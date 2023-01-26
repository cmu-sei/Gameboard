
using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration.Fixtures;

public interface IDataStateBuilder
{
    IDataStateBuilder Add<TEntity>(TEntity entity, Action<TEntity>? entityBuilder = null) where TEntity : class, IEntity;
    // IDataStateBuilder Add<TEntity>(Func<IDataStateBuilder, TEntity> addFunction) where TEntity : class, IEntity;

    // Task Build();
}