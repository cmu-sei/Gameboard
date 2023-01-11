using System.Text.Json;

namespace Gameboard.Tests.Integration.Fixtures;

public static class HttpResponseMessageExtensions
{
    public static async Task<T> WithContentDeserializedAs<T>(this Task<HttpResponseMessage> responseTask, JsonSerializerOptions jsonSerializerOptions) where T : class
    {
        var response = await responseTask;
        response.EnsureSuccessStatusCode();

        var rawResponse = await response.Content.ReadAsStringAsync();
        var deserialized = JsonSerializer.Deserialize<T>(rawResponse, jsonSerializerOptions);

        if (deserialized != null)
        {
            return deserialized;
        }

        throw new ResponseContentDeserializationTypeFailure<T>(rawResponse);
    }
}