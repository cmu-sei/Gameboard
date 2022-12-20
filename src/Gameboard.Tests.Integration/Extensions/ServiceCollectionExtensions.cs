namespace Gameboard.Tests.Integration.Extensions;

internal static class ServiceCollectionExtensions
{
    public static void RemoveService<I>(this IServiceCollection services) where I : class
    {
        var existingService = FindService<I>(services);
        if (existingService != null) services.Remove(existingService);
    }

    public static void ReplaceService<I, C>(this IServiceCollection services) where I : class where C : class, I
    {
        RemoveService<I>(services);
        services.AddSingleton<I, C>();
    }

    public static void ReplaceService<I, C>(this IServiceCollection services, C replacement) where I : class where C : class, I
    {
        RemoveService<I>(services);
        services.AddSingleton<I>(replacement);
    }

    private static ServiceDescriptor? FindService<T>(IServiceCollection services) where T : class
    {
        return services.SingleOrDefault(d => d.ServiceType == typeof(T));
    }
}
