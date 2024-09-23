namespace Gameboard.Api.Tests.Integration.Fixtures;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RemoveService<I>(this IServiceCollection services) where I : class
    {
        var existingService = FindService<I>(services);
        if (existingService != null) services.Remove(existingService);
        return services;
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

    public static IServiceCollection ReplaceService<I, C>(this IServiceCollection services, bool allowMultipleReplace = false) where I : class where C : class, I
    {
        if (allowMultipleReplace)
        {
            RemoveServices<I>(services);
        }
        else
        {
            RemoveService<I>(services);
        }

        services.AddScoped<I, C>();
        return services;
    }

    public static void ReplaceService<I, C>(this IServiceCollection services, C replacement) where I : class where C : class, I
    {
        RemoveService<I>(services);
        services.AddSingleton<I>(sp => replacement);
    }

    public static ServiceDescriptor? FindService<T>(this IServiceCollection services) where T : class
    {
        return services.SingleOrDefault(d => d.ServiceType == typeof(T));
    }

    public static ServiceDescriptor? FindService<I, C>(this IServiceCollection services) where I : class where C : class
    {
        return services.SingleOrDefault(d => d.ServiceType == typeof(I) && d.ImplementationType == typeof(C));
    }

    public static ServiceDescriptor[] FindServices<I>(this IServiceCollection services) where I : class
    {
        return services.Where(d => d.ServiceType == typeof(I)).ToArray();
    }
}
