using System.Reflection;
using AutoFixture.Kernel;

namespace Gameboard.Api.Tests.Integration.Fixtures;

public class IdBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        var name = string.Empty;
        Type argumentType = typeof(object);

        var pi = request as PropertyInfo;
        if (pi != null)
        {
            name = pi.Name;
            argumentType = pi.PropertyType;
        }

        if (request is ParameterInfo rpi)
        {
            name = rpi.Name;
            argumentType = rpi.ParameterType;
        }

        if (!string.IsNullOrEmpty(name) &&
            name.EndsWith("Id") &&
            argumentType == typeof(string))
        {
            var generatedValue = context.Resolve(typeof(string)).ToString()!;
            return generatedValue.Substring(0, Math.Min(40, generatedValue.Length));
        }

        return new NoSpecimen();
    }
}
