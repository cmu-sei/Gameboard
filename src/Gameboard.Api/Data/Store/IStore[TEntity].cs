// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Gameboard.Api.Data.Abstractions;

public interface IStore<TEntity> where TEntity : class, IEntity
{
    IQueryable<TEntity> DbSet { get; }

    Task<bool> AnyAsync();
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
    IQueryable<TEntity> List(string term = null);
    IQueryable<TEntity> ListWithNoTracking();
    Task<TEntity> Create(TEntity entity);
    Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> range);
    Task<bool> Exists(string id);
    Task<TEntity> Retrieve(string id);
    Task<TEntity> Retrieve(string id, Func<IQueryable<TEntity>, IQueryable<TEntity>> includes);
    Task Update(TEntity entity);
    Task Update(IEnumerable<TEntity> range);
    Task Delete(string id);
    Task<int> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder = null);
}
