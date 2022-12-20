using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Gameboard.Tests.Integration.Extensions;

internal static class ObjectExtensions
{
    public static StringContent ToStringContent(this object obj)
        => new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, MediaTypeNames.Application.Json);
}
