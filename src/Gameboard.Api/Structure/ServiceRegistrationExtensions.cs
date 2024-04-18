using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Structure;

internal static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddImplementationsOf<TInterface>(this IServiceCollection serviceCollection)
        => AddImplementationsOf(serviceCollection, typeof(TInterface));

    public static IServiceCollection AddImplementationsOf(this IServiceCollection serviceCollection, Type interfaceType)
    {
        var types = GetRootTypeQuery()
            .Where
            (
                t =>
                    interfaceType.IsGenericTypeDefinition && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType) ||
                    t.GetInterfaces().Contains(interfaceType)
            )
            .Select
            (
                t => new InterfaceTypeMap
                {
                    Interface = interfaceType,
                    Implementation = t.IsGenericType ? t.MakeGenericType(t.GetGenericArguments()) : t
                }
            );

        return RegisterScoped(serviceCollection, types);
    }

    public static IServiceCollection AddInterfacesWithSingleImplementations(this IServiceCollection serviceCollection)
    {
        var interfaceTypes = GetRootQuery()
            .Where(t => t.IsInterface)
            .ToArray();

        var singleInterfaceTypes = GetRootTypeQuery()
            .Where(t => t.GetInterfaces().Length == 1)
            .Where(t => t.GetConstructors().Where(c => c.IsPublic).Any())
            .Where(t => t.GetTypeInfo().GetCustomAttribute<DontBindForDIAttribute>() is null)
            .GroupBy(t => t.GetInterfaces()[0])
            .ToDictionary(t => t.Key, t => t.ToList());

        foreach (var entry in singleInterfaceTypes)
        {
            // if it's a type we want to register and it hasn't already been registered by other logic, add it
            var theInterfaceName = entry.Key.Name;
            var hasInterface = interfaceTypes.Contains(entry.Key);
            var isUnRegistered = serviceCollection.FirstOrDefault(s => s.ServiceType == entry.Key) == null;

            if (interfaceTypes.Contains(entry.Key) && serviceCollection.FirstOrDefault(s => s.ServiceType == entry.Key) == null)
            {
                var isDiHidden = entry.Value[0].GetTypeInfo().GetCustomAttribute<DontBindForDIAttribute>() is not null;

                if (!isDiHidden)
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
        var types = GetRootQuery()
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

    private static IServiceCollection RegisterScoped(IServiceCollection serviceCollection, IEnumerable<InterfaceTypeMap> types)
    {
        foreach (var type in types)
        {
            // okay, to get this, we have to distinguish generic type PARAMETERS versus generic type ARGUMENTS:
            //
            // if a type has concrete generic ARGUMENTS, they appear in the `.GenericTypeArguments` collection (e.g. IStore<Data.Challenge>)`
            // if a type has abstract/interface generic PARAMETERS, they appear in the .GetTypeInfo().GenericTypeParameters collection (e.g. EntityExistsValidator<TEntity>)
            var interfaceGenericParameters = type.Interface.GetTypeInfo().GenericTypeParameters;
            var implementationGenericArgs = type.Implementation.GenericTypeArguments;
            var implementationGenericParams = type.Implementation.GetTypeInfo().GenericTypeParameters;

            // if the interface type and the implementation type have the same number/type of generic PARAMETERS, make a generic version of each and associate them 
            // (e.g. IValidatorService<T> and ValidatorService<T> )
            if (implementationGenericParams.Length == interfaceGenericParameters.Length && type.Interface.IsGenericTypeDefinition)
            {
                var madeImplementationGeneric = type.Implementation.MakeGenericType(implementationGenericParams);
                var madeInterfaceGeneric = type.Interface.MakeGenericType(interfaceGenericParameters);
                serviceCollection.AddScoped(madeInterfaceGeneric, type.Implementation);
                continue;
            }

            // if the implementation type doesn't have any generic args and the interface does, make a generic of the interface's generic parameters and associate
            // (like IGameboardRequestValidator<GetGameStateQuery> and GetGameStateValidator)
            if (implementationGenericArgs.Count() == 0 && implementationGenericParams.Count() == 0 && type.Interface.IsGenericTypeDefinition)
            {
                var matchingInterface = type.Implementation.GetInterfaces().First(i => i.GetGenericTypeDefinition() == type.Interface);
                serviceCollection.AddScoped(matchingInterface, type.Implementation);
                continue;
            }

            // if the the implementation has one or more generic PARAMETERS and they're of a different number than the interface's 
            // (e.g. EntityExistsValidator<TModel, TEntity> vs IGameboardValidator<TModel>) skip for now, because this is quite tricky
            // if (interfaceGenericParameters.Count() > 0 && implementationGenericArgs.Count() != interfaceGenericParameters.Count())
            // {
            //     continue;
            // }

            // otherwise, just punt and add a scoped version of the concrete type (e.g. UserIsPlayingGameValidator)
            if (type.Implementation.GetCustomAttribute<DontBindForDIAttribute>() is null)
                serviceCollection.AddScoped(type.Implementation);
        }

        return serviceCollection;
    }

    private static IEnumerable<Type> GetRootQuery()
      => typeof(Program)
            .Assembly
            .GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("Gameboard"))
            .ToArray();

    private static Type[] GetRootTypeQuery()
     => GetRootQuery()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToArray();

    private class InterfaceTypeMap
    {
        public required Type Interface { get; set; }
        public required Type Implementation { get; set; }
    }
}
