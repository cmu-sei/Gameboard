using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gameboard.Api.Services;

public interface IJsonService
{
    string Serialize<T>(T obj) where T : class;
    T Deserialize<T>(string json) where T : class, new();
}

internal class JsonService : IJsonService
{
    internal static Action<JsonSerializerOptions> BuildJsonSerializerOptions()
    {
        return options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.Converters.Add(new JsonDateTimeConverter());
        };
    }

    internal static JsonService WithGameboardSerializerOptions()
        => new JsonService(BuildJsonSerializerOptions());

    private readonly JsonSerializerOptions _options;

    public JsonService(Action<JsonSerializerOptions> optionsBuilder)
    {
        _options = new JsonSerializerOptions();
        optionsBuilder(_options);
    }

    public JsonService(JsonSerializerOptions options)
    {
        _options = options;
    }

    public T Deserialize<T>(string json) where T : class, new()
    {
        return JsonSerializer.Deserialize<T>(json, _options);
    }

    public string Serialize<T>(T obj) where T : class
    {
        return JsonSerializer.Serialize<T>(obj, _options);
    }
}
