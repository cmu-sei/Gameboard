using Microsoft.AspNetCore.Hosting;

namespace Gameboard.Api;

internal static class IWebHostEnvironmentExtensions
{
    private static string _envDev = "Development";
    private static string _envTest = "Test";

    public static bool IsDevOrTest(this IWebHostEnvironment env)
    {
        return env.EnvironmentName == _envDev || env.EnvironmentName == _envTest;
    }

    public static bool IsDev(this IWebHostEnvironment env)
    {
        return env.EnvironmentName == _envDev;
    }
}
