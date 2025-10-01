// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Reflection;
using AutoFixture.Kernel;

namespace Gameboard.Api.Tests.Shared.Fixtures;

internal class DateTimeOffsetSpecimenBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        var argumentType = typeof(object);

        if (request is PropertyInfo pi)
        {
            argumentType = pi.PropertyType;
        }

        if (request is ParameterInfo rpi)
        {
            argumentType = rpi.ParameterType;
        }

        if (argumentType == typeof(DateTimeOffset))
        {
            return DateTimeOffset.UtcNow.ToUniversalTime();
        }

        return new NoSpecimen();
    }
}
