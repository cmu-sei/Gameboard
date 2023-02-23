using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

internal class GameboardLoggingBuilder
{
    public bool EnableHttpLogging { get; set; } = false;
    public LogLevel LogLevel { get; set; }
}

internal static class LoggingExtensions
{
    public static IApplicationBuilder UseGameboardLogging(this IApplicationBuilder app, Action<GameboardLoggingBuilder> gameboardLoggingBuilder)
    {
        var loggingBuilder = new GameboardLoggingBuilder();
        gameboardLoggingBuilder?.Invoke(loggingBuilder);

        if (loggingBuilder.EnableHttpLogging)
        {
            app.UseHttpLogging();
        }

        return app;
    }
}
