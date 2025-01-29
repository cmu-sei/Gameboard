using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace Gameboard.Api.Common;

public static class DynamicTypeExtensions
{
    /// <summary>
    /// Converts any object of type T to a dynamic (expando) object.
    /// 
    /// If you're using this, it better be for something weird 🤣
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static dynamic ToDynamic<T>(this T obj)
    {
        var dictionary = new ExpandoObject() as IDictionary<string, object>;

        foreach (var property in typeof(T).GetProperties())
        {
            dictionary.Add(property.Name, property.GetValue(obj));
        }

        return dictionary;
    }
}
