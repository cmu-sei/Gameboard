// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

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
