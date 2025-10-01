// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace Gameboard.Api.Common.Services;

// TODO: this is a potentially promising approach to flattening for CSV, but
// the enumerable case is a bit tricky to figure out because we may want "cross-joined"
// data, and doing that via reflection could be pretty crazy. leaving it in because it might be
// useful later, but isn't now.
public interface IFlattenService
{
    IEnumerable<ExpandoObject> Flatten<T>(IEnumerable<T> items) where T : class;
}

internal class FlattenService : IFlattenService
{
    public IEnumerable<ExpandoObject> Flatten<T>(IEnumerable<T> items) where T : class
    {
        var output = new List<ExpandoObject>();

        foreach (var item in items)
        {
            dynamic dItem = new ExpandoObject();
            var propertyValues = GetPropertyValues(item, string.Empty, new Dictionary<string, object>());

            foreach (var propertyValue in propertyValues)
                (dItem as IDictionary<string, object>).Add(propertyValue.Key, propertyValue.Value);

            output.Add(dItem);
        }

        return output;
    }

    private IDictionary<string, object> GetPropertyValues<TItem>(TItem item, string namePath, IDictionary<string, object> propertyValues)
    {
        Console.WriteLine($"FLATTEN::{namePath}");
        if (item is null || IsLeafType(item.GetType()))
        {
            propertyValues.Add(namePath, item);
            return propertyValues;
        }

        if (item.GetType().IsAssignableTo(typeof(IEnumerable)))
        {

        }

        foreach (var prop in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            GetPropertyValues(prop.GetValue(item), $"{namePath}{prop.Name}", propertyValues);
        }

        return propertyValues;
    }

    private bool IsLeafType(Type type)
        => type.IsPrimitive || type.IsEnum || type.IsValueType || type == typeof(string);
}
