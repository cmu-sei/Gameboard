namespace Gameboard.Api.Tests.Integration.Fixtures;

internal static class ServiceCollectionExtensions
{
    public static void RemoveService<I>(this IServiceCollection services) where I : class
    {
        var existingService = FindService<I>(services);
        if (existingService != null) services.Remove(existingService);
    }

    public static void RemoveServices<I>(this IServiceCollection services) where I : class
    {
        var existingServices = FindServices<I>(services);

        foreach (var service in existingServices)
            services.Remove(service);
    }

    public static void RemoveService<I, C>(this IServiceCollection services) where I : class where C : class
    {
        var existingService = FindService<I, C>(services);
        if (existingService != null) services.Remove(existingService);
    }

    public static void ReplaceService<I, C>(this IServiceCollection services, bool allowMultipleReplace = false) where I : class where C : class, I
    {
        if (allowMultipleReplace)
        {
            RemoveServices<I>(services);
        }
        else
        {
            RemoveService<I>(services);
        }

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

    private static ServiceDescriptor? FindService<I, C>(IServiceCollection services) where I : class where C : class
    {
        return services.SingleOrDefault(d => d.ServiceType == typeof(I) && d.ImplementationType == typeof(C));
    }

    private static ServiceDescriptor[] FindServices<I>(IServiceCollection services) where I : class
    {
        return services.Where(d => d.ServiceType == typeof(I)).ToArray();
    }
}
