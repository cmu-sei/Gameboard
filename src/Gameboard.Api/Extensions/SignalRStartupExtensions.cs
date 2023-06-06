using System.Text.Json;
using System.Text.Json.Serialization;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Extensions;

internal static class SignalRStartupExtensions
{
    public static WebApplicationBuilder AddGameboardSignalRServices(this WebApplicationBuilder builder)
    {
        // set log level for SignalR depending on the environment/config
        var logLevel = LogLevel.Error;
        if (builder.Environment.IsDev())
            logLevel = LogLevel.Trace;
        else if (builder.Environment.IsTest())
            logLevel = LogLevel.Information;

        builder.Logging.AddFilter(typeof(Microsoft.AspNetCore.SignalR.Hub).Namespace, logLevel);

        builder.Services
            .AddSingleton<IUserIdProvider, SubjectProvider>()
            .AddSignalR(opt => opt.EnableDetailedErrors = builder.Environment.IsDevOrTest())
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                options.PayloadSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

        return builder;
    }
}
