using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Extensions;

public static class IMvcBuilderExtensions
{
    // this is here because i'm having a weird problem where despite calling AddGameboardJsonOptions on the test app,
    // these options are not registered in services. exposing them statically until i figure that out.
    public static Action<JsonOptions> BuildJsonOptions()
    {
        return options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.JsonSerializerOptions.Converters.Add(new JsonDateTimeConverter());
        };
    }

    public static IMvcBuilder AddGameboardJsonOptions(this IMvcBuilder builder)
    {
        return builder.AddJsonOptions(BuildJsonOptions());
    }
}
