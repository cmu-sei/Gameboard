using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Gameboard.Tests.Integration.Fixtures;

internal static class SerializationExtensions
{
    public static StringContent ToJsonBody(this object obj)
        => new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, MediaTypeNames.Application.Json);

    /// <summary>
    /// Convert an object into querystring format. Currently only works for properties of simple type.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToQueryString(this object obj)
    {
        if (obj == null)
            throw new InvalidOperationException();

        var propertyStrings = obj.GetType()
            .GetProperties()
            .Where(p => p.GetValue(obj) != null)
            .Select(p => $"{p.Name}={HttpUtility.UrlEncode(p.GetValue(obj)!.ToString())}");

        return string.Join('&', propertyStrings);
    }
}
