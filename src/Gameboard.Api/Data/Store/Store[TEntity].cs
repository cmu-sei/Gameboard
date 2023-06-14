// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public class Store<TEntity> : IStore<TEntity> where TEntity : class, IEntity
{
    private readonly IGuidService _guids;

    public Store(IGuidService guids, GameboardDbContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>().AsQueryable();
        _guids = guids;
    }

    public GameboardDbContext DbContext { get; private set; }
    public IQueryable<TEntity> DbSet { get; private set; }

    public virtual IQueryable<TEntity> List(string term = null)
    {
        return DbContext.Set<TEntity>();
    }

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
                entity.Id = Guid.NewGuid().ToString("n");

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

    public virtual async Task Delete(string id)
    {
        var entity = await DbContext.Set<TEntity>().FindAsync(id);

        if (entity is TEntity)
        {
            DbContext.Set<TEntity>().Remove(entity);

            await DbContext.SaveChangesAsync();
        }
    }

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
