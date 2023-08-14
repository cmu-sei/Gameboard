using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Web;
using Gameboard.Api.Services;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal static class SerializationExtensions
{
    public static StringContent ToJsonBody<T>(this T obj) where T : class
    {
        // build gameboard-like serializer options
        var opts = JsonService.GetJsonSerializerOptions();

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

    public static async Task<T?> WithContentDeserializedAs<T>(this Task<HttpResponseMessage> responseTask) where T : class
    {
        var response = await responseTask;
        response.EnsureSuccessStatusCode();

        // we do this to ensure that we're deserializing with the same rules as gameboard is
        var opts = JsonService.GetJsonSerializerOptions();

        var rawResponse = await response.Content.ReadAsStringAsync();

        try
        {
            var deserialized = JsonSerializer.Deserialize<T>(rawResponse, opts);

            if (deserialized != null)
            {
                return deserialized;
            }
        }
        catch (Exception ex)
        {
            throw new ResponseContentDeserializationTypeFailure<T>(rawResponse, ex);
        }

        return default;
    }
}
