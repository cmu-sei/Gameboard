using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Gameboard.Api.Data;

public interface IStore
{
    Task<bool> AnyAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TEntity : class, IEntity;
    Task<TEntity> Create<TEntity>(TEntity entity) where TEntity : class, IEntity;
    Task<TEntity> Create<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class, IEntity;
    Task<int> CountAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class, IEntity;
    Task Delete<TEntity>(string id) where TEntity : class, IEntity;
    Task Delete<TEntity>(params TEntity[] entity) where TEntity : class, IEntity;
    Task DoTransaction(Func<GameboardDbContext, Task> operation, CancellationToken cancellationToken);
    Task<int> ExecuteUpdateAsync<TEntity>
    (
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls
    ) where TEntity : class, IEntity;
    Task<bool> Exists<TEntity>(string id) where TEntity : class, IEntity;
    Task<TEntity> FirstOrDefaultAsync<TEntity>(CancellationToken cancellationToken) where TEntity : class, IEntity;
    Task<TEntity> FirstOrDefaultAsync<TEntity>(StoreTrackingType trackingType, CancellationToken cancellationToken) where TEntity : class, IEntity;
    Task<TEntity> FirstOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TEntity : class, IEntity;
    Task<TEntity> Retrieve<TEntity>(string id, StoreTrackingType trackingType = StoreTrackingType.NoTracking) where TEntity : class, IEntity;
    Task<TEntity> Retrieve<TEntity>(string id, Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder, StoreTrackingType trackingType = StoreTrackingType.NoTracking) where TEntity : class, IEntity;
    Task<IEnumerable<TEntity>> SaveAddRange<TEntity>(params TEntity[] entities) where TEntity : class, IEntity;
    Task SaveRemoveRange<TEntity>(params TEntity[] entities) where TEntity : class, IEntity;
    Task<TEntity> SaveUpdate<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class, IEntity;
    Task SaveUpdateRange<TEntity>(params TEntity[] entities) where TEntity : class, IEntity;
    Task<TEntity> SingleAsync<TEntity>(string id, CancellationToken cancellationToken) where TEntity : class, IEntity;
    Task<TEntity> SingleOrDefaultAsync<TEntity>(CancellationToken cancellationToken) where TEntity : class, IEntity;
    Task<TEntity> SingleOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TEntity : class, IEntity;
    IQueryable<TEntity> WithNoTracking<TEntity>() where TEntity : class, IEntity;
    IQueryable<TEntity> WithNoTrackingAndIdentityResolution<TEntity>() where TEntity : class, IEntity;
    IQueryable<TEntity> WithTracking<TEntity>() where TEntity : class, IEntity;
}

internal class Store : IStore
{
    private readonly GameboardDbContext _dbContext;
    private readonly IGuidService _guids;

    public readonly string MYID;

    public Store(IGuidService guids, GameboardDbContext dbContext)
    {
        _dbContext = dbContext;
        _guids = guids;
        MYID = _guids.GetGuid();
    }

    public Task<bool> AnyAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TEntity : class, IEntity
        => _dbContext.Set<TEntity>().AnyAsync(predicate, cancellationToken);

    public Task<int> CountAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class, IEntity
        => _dbContext.Set<TEntity>().CountAsync(cancellationToken);

    public Task<TEntity> Create<TEntity>(TEntity entity) where TEntity : class, IEntity
        => Create(entity, CancellationToken.None);

    public async Task<TEntity> Create<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class, IEntity
    {
        if (entity.Id.IsEmpty())
            entity.Id = _guids.GetGuid();

        if (!_dbContext.Set<TEntity>().Any(e => e == entity))
            _dbContext.Add(entity);

        await _dbContext.SaveChangesAsync(cancellationToken);
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

    public async Task Delete<TEntity>(params TEntity[] entity) where TEntity : class, IEntity
    {
        _dbContext.RemoveRange(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DoTransaction(Func<GameboardDbContext, Task> operation, CancellationToken cancellationToken)
    {
        using var transaction = _dbContext.Database.BeginTransaction();
        await operation(_dbContext);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<int> ExecuteUpdateAsync<TEntity>
    (
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls
    ) where TEntity : class, IEntity
        => _dbContext
            .Set<TEntity>()
            .Where(predicate)
            .ExecuteUpdateAsync(setPropertyCalls);

    public Task<bool> Exists<TEntity>(string id) where TEntity : class, IEntity
        => _dbContext
            .Set<TEntity>()
            .AnyAsync(e => e.Id == id);

    public Task<TEntity> FirstOrDefaultAsync<TEntity>(CancellationToken cancellationToken) where TEntity : class, IEntity
        => FirstOrDefaultAsync(null as Expression<Func<TEntity, bool>>, StoreTrackingType.NoTracking, cancellationToken);

    public Task<TEntity> FirstOrDefaultAsync<TEntity>(StoreTrackingType trackingType, CancellationToken cancellationToken) where TEntity : class, IEntity
        => FirstOrDefaultAsync(null as Expression<Func<TEntity, bool>>, trackingType, cancellationToken);

    public Task<TEntity> FirstOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TEntity : class, IEntity
        => FirstOrDefaultAsync(predicate, StoreTrackingType.NoTracking, cancellationToken);

    public Task<TEntity> FirstOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, StoreTrackingType trackingType, CancellationToken cancellationToken) where TEntity : class, IEntity
    {
        var query = GetQueryBase<TEntity>(trackingType);

        if (predicate is not null)
            return query.FirstOrDefaultAsync(predicate, cancellationToken);

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<TEntity> Retrieve<TEntity>(string id, StoreTrackingType trackingType = StoreTrackingType.NoTracking) where TEntity : class, IEntity
        => GetQueryBase<TEntity>(trackingType).FirstOrDefaultAsync(e => e.Id == id);

    public Task<TEntity> Retrieve<TEntity>(string id, Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder, StoreTrackingType trackingType = StoreTrackingType.NoTracking) where TEntity : class, IEntity
    {
        var query = GetQueryBase<TEntity>(trackingType);
        query = queryBuilder?.Invoke(query);
        return query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<TEntity> SingleAsync<TEntity>(string id, CancellationToken cancellationToken) where TEntity : class, IEntity
        => GetQueryBase<TEntity>().SingleAsync(e => e.Id == id, cancellationToken);

    public Task<TEntity> SingleOrDefaultAsync<TEntity>(CancellationToken cancellationToken) where TEntity : class, IEntity
        => GetQueryBase<TEntity>().SingleOrDefaultAsync(cancellationToken);

    public Task<TEntity> SingleOrDefaultAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TEntity : class, IEntity
        => GetQueryBase<TEntity>().SingleOrDefaultAsync(predicate, cancellationToken);

    public async Task<IEnumerable<TEntity>> SaveAddRange<TEntity>(params TEntity[] entities) where TEntity : class, IEntity
    {
        _dbContext.AddRange(entities);
        await _dbContext.SaveChangesAsync();

        // detach because of EF stuff
        // TODO: investigate why our store is running afoul of EF attachment stuff
        // may be fixed because we undumbed and made dbcontext transient
        _dbContext.DetachUnchanged();
        return entities;
    }

    public async Task SaveRemoveRange<TEntity>(params TEntity[] entities) where TEntity : class, IEntity
    {
        _dbContext.RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<TEntity> SaveUpdate<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class, IEntity
    {
        await SaveUpdateRange(entity);
        return entity;
    }

    public async Task SaveUpdateRange<TEntity>(params TEntity[] entities) where TEntity : class, IEntity
    {
        foreach (var entity in entities)
        {
            if (_dbContext.Entry(entity).State == EntityState.Detached)
                _dbContext.Attach(entity);
        }

        _dbContext.UpdateRange(entities);
        await _dbContext.SaveChangesAsync();
    }

    public IQueryable<TEntity> WithNoTracking<TEntity>() where TEntity : class, IEntity
        => GetQueryBase<TEntity>(StoreTrackingType.NoTracking);

    public IQueryable<TEntity> WithNoTrackingAndIdentityResolution<TEntity>() where TEntity : class, IEntity
        => GetQueryBase<TEntity>(StoreTrackingType.NoTrackingWithIdentityResolution);

    public IQueryable<TEntity> WithTracking<TEntity>() where TEntity : class, IEntity
        => GetQueryBase<TEntity>(StoreTrackingType.Tracking);

    private IQueryable<TEntity> GetQueryBase<TEntity>(StoreTrackingType trackingType = StoreTrackingType.NoTracking) where TEntity : class, IEntity
    {
        var q = _dbContext.Set<TEntity>().AsQueryable();

        return trackingType switch
        {
            StoreTrackingType.Tracking => q,
            StoreTrackingType.NoTrackingWithIdentityResolution => q.AsNoTrackingWithIdentityResolution(),
            _ => q.AsNoTracking(),
        };
    }
}
