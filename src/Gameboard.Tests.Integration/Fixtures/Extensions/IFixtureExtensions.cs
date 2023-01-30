namespace Gameboard.Tests.Integration.Fixtures;

internal static class IFixtureExtensions
{
    internal static string CreateStringWithLength(this IFixture fixture, int requestedLength)
    {
        string output = "";

        while (output.Length < requestedLength)
        {
            output += fixture.Create<string>();
        }

        return output.Substring(0, requestedLength);
    }
}
