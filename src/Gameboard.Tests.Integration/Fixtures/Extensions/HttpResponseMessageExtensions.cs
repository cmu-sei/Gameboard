using System.Text.Json;
using Gameboard.Api.Services;

namespace Gameboard.Tests.Integration.Fixtures;

public static class HttpResponseMessageExtensions
{
    public static async Task<T?> WithContentDeserializedAs<T>(this Task<HttpResponseMessage> responseTask) where T : class
    {
        var response = await responseTask;
        response.EnsureSuccessStatusCode();

        // we do this to ensure that we're deserializing with the same rules as gameboard is
        var serializerOptions = new JsonSerializerOptions();
        JsonService.BuildJsonSerializerOptions()(serializerOptions);

        var rawResponse = await response.Content.ReadAsStringAsync();

        try
        {
            var deserialized = JsonSerializer.Deserialize<T>(rawResponse, serializerOptions);

            if (deserialized != null)
            {
                return deserialized;
            }
        }
        catch (Exception ex)
        {
            throw new ResponseContentDeserializationTypeFailure<T>(rawResponse, ex);
        }

        return default(T);
    }
}
