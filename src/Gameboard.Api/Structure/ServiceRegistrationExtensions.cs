using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gameboard.Api.Features.ChallengeBonuses;
using Gameboard.Api.Features.GameEngine.Requests;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Structure;

internal static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddImplementationsOf<TInterface>(this IServiceCollection serviceCollection)
        => AddImplementationsOf(serviceCollection, typeof(TInterface));

    public static IServiceCollection AddImplementationsOf(this IServiceCollection serviceCollection, Type interfaceType)
    {
        var thing = GetRootClassQuery()
            .Where
            (
                t =>
                    interfaceType.IsGenericTypeDefinition && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType) ||
                    t.GetInterfaces().Contains(interfaceType)
            );

        var gotit = thing.Contains(typeof(GetSubmissionsRequestValidator));

        var types = GetRootClassQuery()
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

    public static IServiceCollection AddConcretesFromNamespace(this IServiceCollection serviceCollection, string namespaceExact)
        => AddConcretesFromNamespaceCriterion(serviceCollection, t => t.Namespace == namespaceExact);

    public static IServiceCollection AddConcretesFromNamespaceStartsWith(this IServiceCollection serviceCollection, string namespaceStartsWith)
        => AddConcretesFromNamespaceCriterion(serviceCollection, t => t.Namespace.StartsWith(namespaceStartsWith));

    private static IServiceCollection AddConcretesFromNamespaceCriterion(this IServiceCollection serviceCollection, Func<Type, bool> matchCriterion)
    {
        var types = GetRootClassQuery()
            .Where
            (t =>
                !t.IsNested &&
                !string.IsNullOrWhiteSpace(t.Namespace) &&
                t.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Any()
            )
            .Where(matchCriterion)
            .ToArray();

        var thing = types.Contains(typeof(EntityExistsValidator<,>));

        foreach (var type in types)
            serviceCollection.AddScoped(type);

        return serviceCollection;
    }

    private static IEnumerable<ConstructorInfo> GetConstructorsWithParametersFromCurrentAssembly(Type t)
        => t
            .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(c => c.ParametersAreFromExecutingAssembly());

    private static bool ParametersAreFromExecutingAssembly(this ConstructorInfo c)
        => c
            .GetParameters()
            .All
            (
                p =>
                    p.ParameterType.Assembly == Assembly.GetExecutingAssembly() ||
                    p.ParameterType.GetGenericArguments().All(g => g.Assembly == Assembly.GetExecutingAssembly())
            );

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
            if (implementationGenericParams.Count() == interfaceGenericParameters.Count() && type.Interface.IsGenericTypeDefinition)
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
            serviceCollection.AddScoped(type.Implementation);
        }

        return serviceCollection;
    }

    private static Type[] GetRootQuery()
        => typeof(Program)
            .Assembly
            .DefinedTypes
            .Where(t => !t.IsAbstract && !t.IsNested)
            .Where(t => !t.Name.StartsWith("<>f__AnonymousType"))
            .ToArray();

    private static Type[] GetRootClassQuery()
        => GetRootQuery()
            .Where(t => t.IsClass)
            .ToArray();

    private class InterfaceTypeMap
    {
        public required Type Interface { get; set; }
        public required Type Implementation { get; set; }
    }
}
