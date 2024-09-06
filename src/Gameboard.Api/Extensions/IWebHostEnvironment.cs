using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Gameboard.Api;

internal static class IHostEnvironmentExtensions
{
    private static string _envTest = "Test";

    public static bool IsDevOrTest(this IHostEnvironment env)
    {
        return env.IsDevelopment() || env.EnvironmentName == _envTest;
    }

    public static bool IsTest(this IHostEnvironment env)
        => env.EnvironmentName == _envTest;

    public static string GetEnvironmentFriendlyName(this IHostEnvironment env)
    {
        if (env.IsDevelopment())
            return "Dev";
        if (IsTest(env))
            return "Test";
        if (env.IsProduction())
            return "Production";

        return "Other";
    }
}
