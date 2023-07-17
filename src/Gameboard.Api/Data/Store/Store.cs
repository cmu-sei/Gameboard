using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public interface IStore
{
    Task<TEntity> Create<TEntity>(TEntity entity) where TEntity : class, IEntity;
    Task DoTransaction(Func<Task> operation, CancellationToken cancellationToken);
    Task<int> Delete<TEntity>(string id) where TEntity : class, IEntity;
    Task<bool> Exists<TEntity>(string id) where TEntity : class, IEntity;
    IQueryable<TEntity> List<TEntity>() where TEntity : class, IEntity;
    IQueryable<TEntity> ListAsNoTracking<TEntity>() where TEntity : class, IEntity;
    ValueTask<TEntity> Retrieve<TEntity>(string id) where TEntity : class, IEntity;
    Task Update<TEntity>(TEntity entity) where TEntity : class, IEntity;
}

internal class Store : IStore
{
    private readonly GameboardDbContext _dbContext;
    private readonly IGuidService _guids;

    public Store(GameboardDbContext dbContext, IGuidService guids)
    {
        _dbContext = dbContext;
        _guids = guids;
    }

    public async Task DoTransaction(Func<Task> operation, CancellationToken cancellationToken)
    {
        using var transaction = _dbContext.Database.BeginTransaction();
        await operation();
        await transaction.CommitAsync(cancellationToken);
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
            .Where(e => e.Id == id)
            .ExecuteDeleteAsync();
}
