namespace Gameboard.Api.Tests.Integration.Fixtures;

internal static class TestIds
{
    public static string Generate()
        => Guid.NewGuid().ToString("n");
}