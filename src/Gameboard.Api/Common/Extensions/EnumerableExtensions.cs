using System.Collections.Generic;
using System.Linq;

namespace Gameboard.Api.Common;

public static class EnumerableExtensions
{
    public static bool IsNotEmpty<T>(this IEnumerable<T> enumerable)
        => enumerable is not null && enumerable.Any();

    public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
        => !IsNotEmpty(enumerable);

    public static IEnumerable<T> ToEnumerable<T>(this T thing)
        => new T[] { thing };
}
