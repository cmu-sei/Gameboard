using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Gameboard.Api.Common;

public static class CollectionExtensions
{
    public static ICollection<T> ToCollection<T>(this T item)
        => new Collection<T>(new List<T>() { item });
}
