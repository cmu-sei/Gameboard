using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Structure;

internal static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddImplementationsOf<TInterface>(this IServiceCollection serviceCollection) where TInterface : class
    {
        var types = GetRootTypeQuery()
            .Where(t => typeof(TInterface).IsAssignableFrom(t));

        return RegisterScoped(serviceCollection, types);
    }

    public static IServiceCollection AddImplementationsOf(this IServiceCollection serviceCollection, Type type)
    {
        var types = GetRootTypeQuery()
            .Where
            (
                t => t.GetInterfaces().Any
                (
                    i => i.IsGenericType && i.GetGenericTypeDefinition() == type
                )
            );

        return RegisterScoped(serviceCollection, types);
    }

    public static IServiceCollection AddInterfacesWithSingleImplementations(this IServiceCollection serviceCollection)
    {
        var interfaceTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsInterface)
            .ToArray();

        var singleInterfaceTypes = GetRootTypeQuery()
            .Where(t => t.GetInterfaces().Count() == 1)
            .Where(t => t.GetConstructors().Where(c => c.IsPublic).Count() > 0)
            .GroupBy(t => t.GetInterfaces()[0])
            .ToDictionary(t => t.Key, t => t.ToList())
            .Where(entry => entry.Value.Count() == 1);

        var type = typeof(Gameboard.Api.Features.UnityGames.UnityStore);
        var things = type.GetInterfaces();
        var stuff = type.GetInterfaces().Where(i => i.IsInterface);
        var omg = type.Name;

        foreach (var entry in singleInterfaceTypes)
        {
            var intName = entry.Key.Name;
            var implName = entry.Value[0].Name;

            // if it's a type we want to register and it hasn't already been registered by other logic, add it
            if (interfaceTypes.Contains(entry.Key) && serviceCollection.FirstOrDefault(s => s.ServiceType == entry.Key) == null)
            {
                serviceCollection.AddScoped(entry.Key, entry.Value[0]);
            }
        }

        return serviceCollection;
    }

    public static IServiceCollection AddConcretesFromNamespace(this IServiceCollection serviceCollection, string namespaceExact)
        => AddConcretesFromNamespaceCriterion(serviceCollection, t => t.Namespace == namespaceExact);

    public static IServiceCollection AddConcretesFromNamespaceStartsWith(this IServiceCollection serviceCollection, string namespaceStartsWith)
        => AddConcretesFromNamespaceCriterion(serviceCollection, t => t.Namespace.StartsWith(namespaceStartsWith));

    private static IServiceCollection AddConcretesFromNamespaceCriterion(this IServiceCollection serviceCollection, Func<Type, bool> matchCriterion)
    {
        var types = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where
            (t =>
                t.IsClass &&
                !t.IsAbstract &&
                !t.IsNested &&
                !string.IsNullOrWhiteSpace(t.Namespace) &&
                t.GetConstructors().Any(c => c.IsPublic)
            )
            .Where(matchCriterion)
            .ToArray();

        foreach (var type in types)
        {
            serviceCollection.AddScoped(type);
        }

        return serviceCollection;
    }

    private static IServiceCollection RegisterScoped(IServiceCollection serviceCollection, IEnumerable<Type> types)
    {
        foreach (var type in types)
            serviceCollection.AddScoped(type);

        return serviceCollection;
    }

    private static Type[] GetRootTypeQuery()
     => Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass & !t.IsAbstract)
            .ToArray();
}
