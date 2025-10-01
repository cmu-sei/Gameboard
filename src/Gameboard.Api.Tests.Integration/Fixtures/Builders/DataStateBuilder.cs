// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Tests.Shared;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public interface IDataStateBuilder
{
    IDataStateBuilder Add<TEntity>(IFixture fixture) where TEntity : class, IEntity;
    IDataStateBuilder Add<TEntity>(IFixture fixture, Action<TEntity> entityBuilder) where TEntity : class, IEntity;
    IDataStateBuilder Add<TEntity>(TEntity entity, Action<TEntity>? entityBuilder = null) where TEntity : class, IEntity;
    IDataStateBuilder AddRange<TEntity>(ICollection<TEntity> entities) where TEntity : class, IEntity;
}

internal class DataStateBuilder(GameboardDbContext dbContext) : IDataStateBuilder
{
    private readonly GameboardDbContext _dbContext = dbContext;

    public IDataStateBuilder Add<TEntity>(IFixture fixture) where TEntity : class, IEntity
        => Add<TEntity>(fixture, null);

    public IDataStateBuilder Add<TEntity>(IFixture fixture, Action<TEntity>? entityBuilder) where TEntity : class, IEntity
    {
        var entity = fixture.Create<TEntity>() ?? throw new GbAutomatedTestSetupException($"The test fixture can't create entity of type {typeof(TEntity)}");
        entityBuilder?.Invoke(entity);
        entity.Id ??= GuidService.StaticGenerateGuid();

        _dbContext.Add(entity);

        return this;
    }

    public IDataStateBuilder Add<TEntity>(TEntity entity, Action<TEntity>? entityBuilder = null) where TEntity : class, IEntity
    {
        entityBuilder?.Invoke(entity);
        _dbContext.Add(entity);
        return this;
    }

    public IDataStateBuilder AddRange<TEntity>(ICollection<TEntity> entities) where TEntity : class, IEntity
    {
        foreach (var entity in entities)
            Add(entity);

        return this;
    }
}
