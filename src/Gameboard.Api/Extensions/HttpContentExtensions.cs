using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper.Internal;
using Gameboard.Api.Common.Services;

namespace Gameboard.Api;

public static class HttpExtensions
{
    public static async Task<T> DeserializeResponseAs<T>(this Task<HttpResponseMessage> responseTask) where T : class
    {
        var response = await responseTask;

        // we do this to ensure that we're deserializing with the same rules as gameboard is
        var serializerOptions = new JsonSerializerOptions();
        JsonService.BuildJsonSerializerOptions()(serializerOptions);

        var rawResponse = await response.Content.ReadAsStringAsync();

        try
        {
            if (response.IsSuccessStatusCode)
            {
                var deserialized = JsonSerializer.Deserialize<T>(rawResponse, serializerOptions);

                if (deserialized is default(T) && !typeof(T).IsNullableType())
                    throw new NullReferenceException($"Received a null value when attempting to deserialize non-nullable type {typeof(T).Name}");

                return deserialized;
            }
            else
            {
                throw new GameboardException($"The response had an unsuccessful status code ({response.StatusCode}).");
            }
        }
        catch (Exception ex)
        {
            throw new ResponseContentDeserializationTypeFailure<T>(rawResponse, ex);
        }
    }
}
