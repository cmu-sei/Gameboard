using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gameboard.Api.Services;

public interface IJsonService
{
    string Serialize<T>(T obj) where T : class;
    T Deserialize<T>(string json) where T : new();
}

internal class JsonService : IJsonService
{
    public JsonService() { }

    internal static Action<JsonSerializerOptions> BuildJsonSerializerOptions()
    {
        return options =>
        {
            options.PropertyNameCaseInsensitive = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.Converters.Add(new JsonDateTimeOffsetConverter());
        };
    }

    internal static JsonService WithGameboardSerializerOptions()
        => new JsonService(BuildJsonSerializerOptions());

    public JsonSerializerOptions Options { get; private set; }

    public JsonService(Action<JsonSerializerOptions> optionsBuilder)
    {
        Options = new JsonSerializerOptions();
        optionsBuilder(Options);
    }

    public JsonService(JsonSerializerOptions options)
    {
        Options = options;
    }

    public T Deserialize<T>(string json) where T : new()
    {
        if (json.IsEmpty())
            return default(T);

        return JsonSerializer.Deserialize<T>(json, Options);
    }

    public string Serialize<T>(T obj) where T : class
    {
        return JsonSerializer.Serialize<T>(obj, Options);
    }
}
