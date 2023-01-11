using Gameboard.Api.Data;

namespace Gameboard.Tests.Integration.Fixtures;

internal class DataStateBuilder<TDbContext> : IDataStateBuilder where TDbContext : GameboardDbContext
{
    private readonly TDbContext _DbContext;
    private readonly IList<IEntity> _ToBeAdded = new List<IEntity>();

    public DataStateBuilder(TDbContext dbContext)
    {
        _DbContext = dbContext;
    }

    public IDataStateBuilder Add<TEntity>(TEntity entity, Action<TEntity>? entityBuilder = null) where TEntity : class, IEntity
    {
        entityBuilder?.Invoke(entity);
        _DbContext.Add(entity);
        return this;
    }
}