using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Gameboard.Api;

internal static class IWebHostEnvironmentExtensions
{
    private static string _envTest = "Test";

    public static bool IsDevOrTest(this IWebHostEnvironment env)
    {
        return env.IsDevelopment() || env.EnvironmentName == _envTest;
    }

    public static bool IsTest(this IWebHostEnvironment env)
        => env.EnvironmentName == _envTest;

    public static string GetEnvironmentFriendlyName(this IWebHostEnvironment env)
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
