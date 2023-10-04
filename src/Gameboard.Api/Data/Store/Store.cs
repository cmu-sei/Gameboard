using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Query;

namespace Gameboard.Api.Data;

public interface IStore
{
    Task<bool> AnyAsync<TEntity>() where TEntity : class, IEntity;
    Task<bool> AnyAsync<TEntity>(Expression<Func<TEntity, bool>> predicate) where TEntity : class, IEntity;
    Task<int> CountAsync<TEntity>(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder) where TEntity : class, IEntity;
    Task<TEntity> Create<TEntity>(TEntity entity) where TEntity : class, IEntity;
    Task Delete<TEntity>(string id) where TEntity : class, IEntity;
    Task Delete<TEntity>(params TEntity[] entity) where TEntity : class, IEntity;
    Task DoTransaction(Func<Task> operation, CancellationToken cancellationToken);
    Task<int> ExecuteUpdateAsync<TEntity>
    (
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls
    ) where TEntity : class, IEntity;
    Task<bool> Exists<TEntity>(string id) where TEntity : class, IEntity;
    Task<TEntity> FirstOrDefaultAsync<TEntity>(CancellationToken cancellationToken) where TEntity : class, IEntity;
    Task<TEntity> FirstOrDefaultAsync<TEntity>(bool enableTracking, CancellationToken cancellationToken) where TEntity : class, IEntity;
    Task<TEntity> FirstOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TEntity : class, IEntity;
    IQueryable<TEntity> List<TEntity>(bool enableTracking = false) where TEntity : class, IEntity;
    Task<TEntity> Retrieve<TEntity>(string id, bool enableTracking = false) where TEntity : class, IEntity;
    Task<TEntity> Retrieve<TEntity>(string id, Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder, bool enableTracking = false) where TEntity : class, IEntity;
    Task Save<TEntity>(params TEntity[] entities) where TEntity : class, IEntity;
    Task<TEntity> SingleAsync<TEntity>(string id, CancellationToken cancellationToken) where TEntity : class, IEntity;
    Task<TEntity> SingleOrDefaultAsync<TEntity>(CancellationToken cancellationToken) where TEntity : class, IEntity;
    Task<TEntity> SingleOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TEntity : class, IEntity;
    IQueryable<TEntity> WithNoTracking<TEntity>() where TEntity : class, IEntity;
    IQueryable<TEntity> WithTracking<TEntity>() where TEntity : class, IEntity;
    Task<TEntity> Update<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class, IEntity;
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
        if (entity.Id.IsEmpty())
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

    public Task Delete<TEntity>(params TEntity[] entity) where TEntity : class, IEntity
    {
        _dbContext.RemoveRange(entity);
        return _dbContext.SaveChangesAsync();
    }

    public async Task DoTransaction(Func<Task> operation, CancellationToken cancellationToken)
    {
        using var transaction = _dbContext.Database.BeginTransaction();
        await operation();
        await transaction.CommitAsync(cancellationToken);
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

    public Task<TEntity> FirstOrDefaultAsync<TEntity>(CancellationToken cancellationToken) where TEntity : class, IEntity
        => FirstOrDefaultAsync(null as Expression<Func<TEntity, bool>>, false, cancellationToken);

    public Task<TEntity> FirstOrDefaultAsync<TEntity>(bool enableTracking, CancellationToken cancellationToken) where TEntity : class, IEntity
        => FirstOrDefaultAsync(null as Expression<Func<TEntity, bool>>, enableTracking, cancellationToken);

    public Task<TEntity> FirstOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TEntity : class, IEntity
        => FirstOrDefaultAsync(predicate, false, cancellationToken);

    public Task<TEntity> FirstOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, bool enableTracking, CancellationToken cancellationToken) where TEntity : class, IEntity
    {
        var query = GetQueryBase<TEntity>(enableTracking);

        if (predicate is not null)
            return query.FirstOrDefaultAsync(predicate, cancellationToken);

        return query.FirstOrDefaultAsync(cancellationToken);
    }

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

    public Task Save<TEntity>(params TEntity[] entities) where TEntity : class, IEntity
    {
        _dbContext.AddRange(entities);
        return _dbContext.SaveChangesAsync();
    }

    public Task<TEntity> SingleAsync<TEntity>(string id, CancellationToken cancellationToken) where TEntity : class, IEntity
        => GetQueryBase<TEntity>().SingleAsync(e => e.Id == id, cancellationToken);

    public Task<TEntity> SingleOrDefaultAsync<TEntity>(CancellationToken cancellationToken) where TEntity : class, IEntity
        => GetQueryBase<TEntity>().SingleOrDefaultAsync(cancellationToken);

    public Task<TEntity> SingleOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TEntity : class, IEntity
        => GetQueryBase<TEntity>().SingleOrDefaultAsync(predicate, cancellationToken);

    public async Task<TEntity> Update<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class, IEntity
    {
        if (_dbContext.Entry(entity).State == EntityState.Detached)
            _dbContext.Attach(entity);

        _dbContext.Update(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public IQueryable<TEntity> WithNoTracking<TEntity>() where TEntity : class, IEntity
        => GetQueryBase<TEntity>(enableTracking: false);

    public IQueryable<TEntity> WithTracking<TEntity>() where TEntity : class, IEntity
        => GetQueryBase<TEntity>(enableTracking: true);

    private IQueryable<TEntity> GetQueryBase<TEntity>(bool enableTracking = false) where TEntity : class, IEntity
    {
        var query = _dbContext.Set<TEntity>().AsQueryable();

        if (!enableTracking)
            query = query.AsNoTracking();

        return query;
    }
}
