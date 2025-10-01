// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gameboard.Api.Data;

namespace Gameboard.Api.Common;

public static class IEntityExtensions
{
    public static SimpleEntity ToSimpleEntity<T>(this T entity, Func<T, string> nameProperty) where T : IEntity
        => new() { Id = entity.Id, Name = nameProperty(entity) };

    public static SimpleEntity ToSimpleEntity<T>(this T entity, Func<T, string> idProperty, Func<T, string> nameProperty)
        => new() { Id = idProperty(entity), Name = nameProperty(entity) };
}
