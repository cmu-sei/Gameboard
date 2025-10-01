// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using FakeItEasy.Sdk;

namespace Gameboard.Api.Tests.Unit;

public static class FakeBuilder
{
    /// <summary>
    /// Constructs and automatically hydrates a T with fakes for each of its constructor parameters.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>A T constructed with A.Fake<Y>() for each of its constructor parameters</returns>
    public static T BuildMeA<T>(params object[] fixedParameterValues) where T : class
    {
        if (fixedParameterValues.GroupBy(v => v.GetType()).Count() < fixedParameterValues.Count())
        {
            throw new GameboardApiTestsFakingException($"Can't FakeBuild a {typeof(T).Name} because two or more of the supplied parameter values are of the same type.");
        }

        var constructors = typeof(T).GetConstructors();

        if (constructors.Count() > 1)
            throw new GameboardApiTestsFakingException($"Can't FakeBuild a {typeof(T).Name} because it has multiple constructors.");

        var constructor = constructors.First();
        var parameters = constructor.GetParameters();

        if (parameters.GroupBy(v => v.ParameterType).Count() < parameters.Count())
            throw new GameboardApiTestsFakingException($"Can't FakeBuild a {typeof(T).Name} because its constructor has multiple parameters of the same type.");

        var parametersToConstructor = new List<object>();
        foreach (var parameter in parameters)
        {
            var fixedParameterValue = fixedParameterValues.FirstOrDefault(v => parameter.ParameterType.IsAssignableFrom(v.GetType()));

            if (fixedParameterValue != null)
                parametersToConstructor.Add(fixedParameterValue);
            else
                parametersToConstructor.Add(Create.Fake(parameter.ParameterType));
        }

        var retVal = constructor.Invoke(parametersToConstructor.ToArray()) as T;

        if (retVal == null)
            throw new GameboardApiTestsFakingException($"Can't FakeBuild a {typeof(T).Name} - Constructor returned null");

        return retVal;
    }
}
