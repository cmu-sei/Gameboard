using Gameboard.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Extensions;

public static class IMvcBuilderExtensions
{
    internal static IMvcBuilder AddGameboardJsonOptions(this IMvcBuilder builder)
    {
        return builder.AddJsonOptions(jsonOptions =>
        {
            JsonService.BuildJsonSerializerOptions()(jsonOptions.JsonSerializerOptions);
        });
    }
}
