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

    public static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions();
        BuildJsonSerializerOptions()(options);
        return options;
    }

    public static Action<JsonSerializerOptions> BuildJsonSerializerOptions()
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

    public static JsonService WithGameboardSerializerOptions()
        => new(BuildJsonSerializerOptions());

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
            return default;

        return JsonSerializer.Deserialize<T>(json, Options);
    }

    public string Serialize<T>(T obj) where T : class
    {
        return JsonSerializer.Serialize<T>(obj, Options);
    }
}
