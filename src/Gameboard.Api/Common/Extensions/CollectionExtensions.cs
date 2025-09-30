// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Gameboard.Api.Common;

public static class CollectionExtensions
{
    public static ICollection<T> ToCollection<T>(this T item)
        => new Collection<T>(new List<T>() { item });
}
