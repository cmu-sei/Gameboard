using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Gameboard.Api.Data;

public interface IStore
{
    Task<bool> AnyAsync<TEntity>() where TEntity : class, IEntity;
    Task<bool> AnyAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, IEntity;
    Task<int> CountAsync<TEntity>(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder) where TEntity : class, IEntity;
    Task<TEntity> Create<TEntity>(TEntity entity) where TEntity : class, IEntity;
    Task Delete<TEntity>(string id) where TEntity : class, IEntity;
    Task<int> ExecuteUpdateAsync<TEntity>
    (
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls
    ) where TEntity : class, IEntity;
    Task<bool> Exists<TEntity>(string id) where TEntity : class, IEntity;
    IQueryable<TEntity> List<TEntity>(bool enableTracking = false) where TEntity : class, IEntity;
    Task<TEntity> Retrieve<TEntity>(string id, bool enableTracking = false) where TEntity : class, IEntity;
    Task<TEntity> Retrieve<TEntity>(string id, Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder, bool enableTracking = false) where TEntity : class, IEntity;
    Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class, IEntity;
}

internal class Store : IStore
{
    private readonly IGuidService _guids;
    private readonly GameboardDbContext _dbContext;

    public Store(IGuidService guids, GameboardDbContext dbContext)
    {
        _dbContext = dbContext;
        _guids = guids;
    }

    public Task<bool> AnyAsync<TEntity>() where TEntity : class, IEntity
        => _dbContext.Set<TEntity>().AnyAsync();

    public Task<bool> AnyAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, IEntity
        => _dbContext.Set<TEntity>().AnyAsync(predicate);

    public async Task<int> CountAsync<TEntity>(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder) where TEntity : class, IEntity
    {
        var query = _dbContext.Set<TEntity>().AsNoTracking();
        query = queryBuilder?.Invoke(query);
        return await query.CountAsync();
    }

    public async Task<TEntity> Create<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = _guids.GetGuid();

        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync();

        return entity;
    }

    public async Task Delete<TEntity>(string id) where TEntity : class, IEntity
    {
        var rowsAffected = await _dbContext
            .Set<TEntity>()
            .Where(e => e.Id == id)
            .ExecuteDeleteAsync();

        if (rowsAffected != 1)
            throw new GameboardException($"""Delete of entity type {typeof(TEntity)} with id "{id}" affected {rowsAffected} rows (expected 1).""");
    }

    public Task<int> ExecuteUpdateAsync<TEntity>
    (
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls
    ) where TEntity : class, IEntity
    {
        return _dbContext
            .Set<TEntity>()
            .Where(predicate)
            .ExecuteUpdateAsync(setPropertyCalls);
    }

    public Task<bool> Exists<TEntity>(string id) where TEntity : class, IEntity
        => _dbContext
            .Set<TEntity>()
            .AnyAsync(e => e.Id == id);

    public IQueryable<TEntity> List<TEntity>(bool enableTracking = false) where TEntity : class, IEntity
        => GetQueryBase<TEntity>(enableTracking);

    public Task<TEntity> Retrieve<TEntity>(string id, bool enableTracking = false) where TEntity : class, IEntity
        => GetQueryBase<TEntity>(enableTracking).FirstOrDefaultAsync(e => e.Id == id);

    public Task<TEntity> Retrieve<TEntity>(string id, Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder, bool enableTracking = false) where TEntity : class, IEntity
    {
        var query = GetQueryBase<TEntity>(enableTracking);
        query = queryBuilder?.Invoke(query);
        return query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<TEntity> Update<TEntity>(TEntity entity) where TEntity : class, IEntity
    {
        if (_dbContext.Entry(entity).State == EntityState.Detached)
            _dbContext.Attach(entity);

        _dbContext.Update(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    private IQueryable<TEntity> GetQueryBase<TEntity>(bool enableTracking = false) where TEntity : class, IEntity
    {
        var query = _dbContext.Set<TEntity>().AsQueryable();

        if (!enableTracking)
            query = query.AsNoTracking();

        return query;
    }
}
