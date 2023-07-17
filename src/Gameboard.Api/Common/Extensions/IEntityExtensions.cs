using System;
using Gameboard.Api.Data;

namespace Gameboard.Api.Common;

public static class IEntityExtensions
{
    public static SimpleEntity ToSimpleEntity<T>(this T entity, Func<T, string> nameProperty) where T : IEntity
        => new() { Id = entity.Id, Name = nameProperty(entity) };
}
