using System.Collections.Generic;

namespace Gameboard.Api.Common;

public static class IDictionaryExtensions
{
    public static IDictionary<K, V> EnsureKey<K, V>(this IDictionary<K, V> dictionary, K key, V defaultValue)
    {
        if (!dictionary.ContainsKey(key))
            dictionary.Add(key, defaultValue ?? default);

        return dictionary;
    }
}
