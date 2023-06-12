using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

internal interface IStore
{
    Task<TEntity> Create<TEntity>(TEntity entity) where TEntity : class, IEntity;
    Task<int> Delete<TEntity>(string id) where TEntity : class, IEntity;
    Task<bool> Exists<TEntity>(string id) where TEntity : class, IEntity;
    IQueryable<TEntity> List<TEntity>() where TEntity : class, IEntity;
    IQueryable<TEntity> ListAsNoTracking<TEntity>() where TEntity : class, IEntity;
    ValueTask<TEntity> Retrieve<TEntity>(string id) where TEntity : class, IEntity;
    Task Update<TEntity>(TEntity entity) where TEntity : class, IEntity;
}

internal sealed class Store : IStore
{
    private readonly GameboardDbContext _dbContext;
    private readonly IGuidService _guids;

    public Store(GameboardDbContext dbContext, IGuidService guids)
    {
        _dbContext = dbContext;
        _guids = guids;
    }

    public IQueryable<TEntity> List<TEntity>() where TEntity : class, IEntity
    {
        return _dbContext.Set<TEntity>();
    }

    public IQueryable<TEntity> ListAsNoTracking<TEntity>() where TEntity : class, IEntity
    {
        return _dbContext.Set<TEntity>().AsNoTracking();
    }

    public async Task<TEntity> Create<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = _guids.GetGuid();

        _dbContext.Add(entity);

        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public Task<bool> Exists<TEntity>(string id) where TEntity : class, IEntity
    {
        return ListAsNoTracking<TEntity>().AnyAsync();
    }

    public ValueTask<TEntity> Retrieve<TEntity>(string id) where TEntity : class, IEntity
        => _dbContext.Set<TEntity>().FindAsync(id);

    public Task Update<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        _dbContext.Update(entity);
        return _dbContext.SaveChangesAsync();
    }

    public Task<int> Delete<TEntity>(string id) where TEntity : class, IEntity
        => _dbContext.Set<TEntity>()
            .Where<TEntity>(e => e.Id == id)
            .ExecuteDeleteAsync<TEntity>();
}
