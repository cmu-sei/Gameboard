using Gameboard.Api.Data;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class DataStateBuilder : IDataStateBuilder
{
    private readonly GameboardDbContext _DbContext;

    public DataStateBuilder(GameboardDbContext dbContext) => _DbContext = dbContext;

    public IDataStateBuilder Add<TEntity>(TEntity entity, Action<TEntity>? entityBuilder = null) where TEntity : class, IEntity
    {
        entityBuilder?.Invoke(entity);

        try
        {
            _DbContext.Add(entity);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ex:{ex.Message}");
        }
        return this;
    }

    public IDataStateBuilder AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class, IEntity
    {
        foreach (var entity in entities)
            Add(entity);

        return this;
    }
}
