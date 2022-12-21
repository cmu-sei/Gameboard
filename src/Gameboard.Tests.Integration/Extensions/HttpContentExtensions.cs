using System.Text.Json;

namespace Gameboard.Tests.Integration.Extensions;

internal static class HttpContentExtensions
{
    public static async Task<T?> JsonDeserializeAsync<T>(this HttpContent content, JsonSerializerOptions opts) where T : class
    {
        var json = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, opts);
    }
}
