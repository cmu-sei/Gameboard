using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration.Fixtures;

internal class DataStateBuilder<TDbContext> : IDataStateBuilder where TDbContext : GameboardDbContext
{
    private readonly TDbContext _DbContext;

    public DataStateBuilder(TDbContext dbContext)
    {
        _DbContext = dbContext;
    }

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
