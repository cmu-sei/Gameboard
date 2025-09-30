// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gameboard.Api.Data;

public static class ModelCreationExtensions
{
    public static PropertyBuilder HasStandardGuidLength(this PropertyBuilder builder)
        => builder.HasMaxLength(40);

    public static PropertyBuilder HasStandardNameLength(this PropertyBuilder builder)
        => builder.HasMaxLength(64);

    public static PropertyBuilder HasStandardUrlLength(this PropertyBuilder builder)
        => builder.HasMaxLength(200);

    public static void HasGameboardJsonColumn<TEntity, TProperty>(this EntityTypeBuilder<TEntity> entityTypeBuilder, Expression<Func<TEntity, TProperty>> propertyExpression, DatabaseFacade db)
        where TEntity : class, IEntity
        where TProperty : class
    {

        if (db.IsNpgsql())
        {
            entityTypeBuilder.Property(propertyExpression).HasColumnType("jsonb");
            return;
        }

        throw new NotImplementedException($"""Json column configuration has not been created for database provider: {db.ProviderName}""");
    }
}
