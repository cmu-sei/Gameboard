// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;

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

        return Register(serviceCollection, types);
    }

    public static IServiceCollection AddInterfacesWithSingleImplementations(this IServiceCollection serviceCollection)
    {
        var interfaceTypes = GetRootQuery()
            .Where(t => t.IsInterface)
            .ToArray();

        var singleInterfaceTypes = GetRootTypeQuery()
            .Where(t => t.GetInterfaces().Length == 1)
            .Where(t => t.GetConstructors().Where(c => c.IsPublic).Any())
            .Where(t => t.GetTypeInfo().GetCustomAttribute<DIIgnoreAttribute>() is null)
            .GroupBy(t => t.GetInterfaces()[0])
            .ToDictionary(t => t.Key, t => t.ToList());

        var imageStoreType = singleInterfaceTypes.Where(t => t.Key.Name.ToLower().Contains("imagestore"));

        foreach (var entry in singleInterfaceTypes)
        {
            // if it's a type we want to register and it hasn't already been registered by other logic, add it
            var theInterfaceName = entry.Key.Name;
            var hasInterface = interfaceTypes.Contains(entry.Key);
            var isUnRegistered = serviceCollection.FirstOrDefault(s => s.ServiceType == entry.Key) == null;

            if (interfaceTypes.Contains(entry.Key) && serviceCollection.FirstOrDefault(s => s.ServiceType == entry.Key) == null)
            {
                Register(serviceCollection, entry.Key, entry.Value[0]);
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
            Register(serviceCollection, type);
        }

        return serviceCollection;
    }

    private static IServiceCollection Register(IServiceCollection serviceCollection, IEnumerable<InterfaceTypeMap> types)
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
            // (e.g. IValidatorService<T> and ValidatorService<T>)
            if (implementationGenericParams.Length == interfaceGenericParameters.Length && type.Interface.IsGenericTypeDefinition)
            {
                var madeImplementationGeneric = type.Implementation.MakeGenericType(implementationGenericParams);
                var madeInterfaceGeneric = type.Interface.MakeGenericType(interfaceGenericParameters);
                Register(serviceCollection, madeInterfaceGeneric, type.Implementation);
                continue;
            }

            // if the implementation type doesn't have any generic args and the interface does, make a generic of the interface's generic parameters and associate
            // (like IGameboardRequestValidator<GetGameStateQuery> and GetGameStateValidator)
            if (implementationGenericArgs.Length == 0 && implementationGenericParams.Length == 0 && type.Interface.IsGenericTypeDefinition)
            {
                var matchingInterface = type.Implementation.GetInterfaces().First(i => i.GetGenericTypeDefinition() == type.Interface);
                Register(serviceCollection, matchingInterface, type.Implementation);
                continue;
            }

            // otherwise, just punt and add a scoped version of the concrete type (e.g. UserIsPlayingGameValidator)
            Register(serviceCollection, type.Implementation);

        }

        return serviceCollection;
    }

    private static IServiceCollection Register(IServiceCollection serviceCollection, Type implementationType)
        => Register(serviceCollection, null, implementationType);

    private static IServiceCollection Register(IServiceCollection serviceCollection, Type interfaceType, Type implementationType)
    {
        ArgumentNullException.ThrowIfNull(implementationType);

        if (implementationType.HasAttribute<DIIgnoreAttribute>())
            return serviceCollection;

        var asTransient = implementationType.HasAttribute<DIAsTransientAttribute>();

        if (interfaceType is not null)
        {
            if (asTransient)
                serviceCollection.AddTransient(interfaceType, implementationType);
            else
                serviceCollection.AddScoped(interfaceType, implementationType);
        }
        else
        {
            if (asTransient)
                serviceCollection.AddTransient(implementationType);
            else
                serviceCollection.AddScoped(implementationType);
        }

        return serviceCollection;
    }

    private static Type[] GetRootQuery()
      => typeof(Program)
            .Assembly
            .GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("Gameboard"))
            .ToArray();

    private static Type[] GetRootTypeQuery()
     => [.. GetRootQuery().Where(t => t.IsClass && !t.IsAbstract)];

    private class InterfaceTypeMap
    {
        public required Type Interface { get; set; }
        public required Type Implementation { get; set; }
    }
}
