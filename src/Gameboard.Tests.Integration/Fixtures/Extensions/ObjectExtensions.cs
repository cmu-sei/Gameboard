using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Gameboard.Tests.Integration.Fixtures;

internal static class ObjectExtensions
{
    public static StringContent ToJsonBody(this object obj)
        => new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, MediaTypeNames.Application.Json);
}
