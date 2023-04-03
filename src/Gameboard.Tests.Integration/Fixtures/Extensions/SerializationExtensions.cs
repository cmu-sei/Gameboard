using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Web;
using Gameboard.Api.Services;

namespace Gameboard.Tests.Integration.Fixtures;

internal static class SerializationExtensions
{
    public static StringContent ToJsonBody<T>(this T obj) where T : class
    {
        // build gameboard-like serializer options
        var opts = new JsonSerializerOptions();
        var builder = JsonService.BuildJsonSerializerOptions();
        builder(opts);

        // serialize and go
        return new StringContent(JsonSerializer.Serialize(obj, opts), Encoding.UTF8, MediaTypeNames.Application.Json);
    }

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
