// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public class Store<TEntity> : IStore<TEntity> where TEntity : class, IEntity
{
    private readonly GameboardDbContext _dbContext;
    private readonly IGuidService _guids;

    public Store(IDbContextFactory<GameboardDbContext> dbContextFactory, IGuidService guids)
    {
        _dbContext = dbContextFactory.CreateDbContext();
        DbSet = _dbContext.Set<TEntity>().AsQueryable();
        _guids = guids;
    }

    public IQueryable<TEntity> DbSet { get; private set; }

    public Task<bool> AnyAsync()
        => DbSet.AnyAsync();

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        => DbSet.AnyAsync(predicate);

    public virtual IQueryable<TEntity> List(string term = null)
        => DbSet;

    public virtual IQueryable<TEntity> ListAsNoTracking()
    {
        return DbSet.AsNoTracking();
    }

    public IQueryable<TEntity> ListWithNoTracking()
        => DbSet
            .AsNoTracking()
            .AsQueryable();

    public virtual async Task<TEntity> Create(TEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = _guids.GetGuid();

        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync();

        return entity;
    }

    public virtual async Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> range)
    {
        foreach (var entity in range)
            if (string.IsNullOrWhiteSpace(entity.Id))
                entity.Id = _guids.GetGuid();

        _dbContext.AddRange(range);

        await _dbContext.SaveChangesAsync();

        return range;
    }

    public virtual async Task<bool> Exists(string id)
    {
        return (await List().CountAsync(e => e.Id == id)) > 0;
    }

    public virtual Task<TEntity> Retrieve(string id)
        => Retrieve(id, null);

    public virtual async Task<TEntity> Retrieve(string id, Func<IQueryable<TEntity>, IQueryable<TEntity>> includes)
    {
        if (includes != null)
        {
            var query = includes(_dbContext.Set<TEntity>());
            return await query
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        return await _dbContext.Set<TEntity>().FindAsync(id);
    }

    public virtual async Task Update(TEntity entity)
    {
        _dbContext.Update(entity);

        await _dbContext.SaveChangesAsync();
    }

    public virtual async Task Update(IEnumerable<TEntity> range)
    {
        _dbContext.UpdateRange(range);

        await _dbContext.SaveChangesAsync();
    }

    public async Task Delete(string id)
        => await DbSet
            .Where(e => e.Id == id)
            .ExecuteDeleteAsync();

    public virtual async Task<int> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder = null)
    {
        var query = DbSet.AsNoTracking();

        if (queryBuilder != null)
        {
            query = queryBuilder(query);
        }

        return await query.CountAsync();
    }
}
