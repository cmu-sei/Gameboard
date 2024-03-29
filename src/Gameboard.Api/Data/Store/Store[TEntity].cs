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
    private readonly IGuidService _guids;

    public Store(GameboardDbContext dbContext, IGuidService guids)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>().AsQueryable();
        _guids = guids;
    }

    public GameboardDbContext DbContext { get; private set; }
    public IQueryable<TEntity> DbSet { get; private set; }

    public Task<bool> AnyAsync()
        => DbContext.Set<TEntity>().AnyAsync();

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        => DbContext.Set<TEntity>().AnyAsync(predicate);

    public virtual IQueryable<TEntity> List(string term = null)
    {
        return DbContext.Set<TEntity>();
    }

    public virtual IQueryable<TEntity> ListAsNoTracking()
    {
        return DbContext.Set<TEntity>().AsNoTracking();
    }

    public IQueryable<TEntity> ListWithNoTracking()
        => DbContext
            .Set<TEntity>()
            .AsNoTracking()
            .AsQueryable();

    public virtual async Task<TEntity> Create(TEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
            entity.Id = _guids.GetGuid();

        DbContext.Add(entity);
        await DbContext.SaveChangesAsync();

        return entity;
    }

    public virtual async Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> range)
    {
        foreach (var entity in range)
            if (string.IsNullOrWhiteSpace(entity.Id))
                entity.Id = _guids.GetGuid();

        DbContext.AddRange(range);

        await DbContext.SaveChangesAsync();

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
            var query = includes(DbContext.Set<TEntity>());
            return await query
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        return await DbContext.Set<TEntity>().FindAsync(id);
    }

    public virtual async Task Update(TEntity entity)
    {
        DbContext.Update(entity);

        await DbContext.SaveChangesAsync();
    }

    public virtual async Task Update(IEnumerable<TEntity> range)
    {
        DbContext.UpdateRange(range);

        await DbContext.SaveChangesAsync();
    }

    public async Task Delete(string id)
        => await DbContext
            .Set<TEntity>()
            .Where(e => e.Id == id)
            .ExecuteDeleteAsync();

    public virtual async Task<int> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder = null)
    {
        var query = DbContext.Set<TEntity>().AsNoTracking();

        if (queryBuilder != null)
        {
            query = queryBuilder(query);
        }

        return await query.CountAsync();
    }
}
