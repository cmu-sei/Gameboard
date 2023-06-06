using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gameboard.Api.Common.Services;

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
            options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.Converters.Add(new JsonDateTimeConverter());
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

    public T Deserialize<T>(string json) where T : class, new()
    {
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    public string Serialize<T>(T obj) where T : class
    {
        return JsonSerializer.Serialize<T>(obj, Options);
    }
}
