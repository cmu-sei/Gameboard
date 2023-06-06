using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Games;

namespace Gameboard.Api.Hubs;

public class SignalRInvocationPayloadConverter<TModel> : JsonConverter<TModel> where TModel : class, new()
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(string);
    }

    public override TModel Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) => JsonService
                .WithGameboardSerializerOptions()
                .Deserialize<TModel>(JsonDocument.ParseValue(ref reader).RootElement.Clone().ToString());

    public override void Write(Utf8JsonWriter writer, TModel value, JsonSerializerOptions options)
    {
        // JsonService
        //     .WithGameboardSerializerOptions()
        //     .Serialize(value)
        //     .
        throw new NotImplementedException();
    }

    // private class StringToModelConverter<TModel> : JsonConverter<TModel> where TModel : class, new()
    // {
    //     public override TModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    //     {
    //         var result = StringToModel(reader);
    //         return result;
    //     }

    //     public override void Write(Utf8JsonWriter writer, TModel value, JsonSerializerOptions options)
    //     {
    //         throw new NotImplementedException();
    //     }

    //     private static TModel StringToModel(Utf8JsonReader reader)
    //     {
    //         var jsonService = JsonService.WithGameboardSerializerOptions();
    //         var jsonText = "";
    //         using (var jsonDocument = JsonDocument.ParseValue(ref reader))
    //         {
    //             jsonText = jsonDocument.RootElement.GetRawText();
    //         }

    //         Console.WriteLine(jsonText);
    //         return jsonService.Deserialize<TModel>(jsonText);
    //     }
    // }
}
