using Microsoft.Extensions.Primitives;

internal class GameboardIntegrationTestException : Exception
{
    public GameboardIntegrationTestException(string message) : base(message) { }
}

internal class CantResolveAuthUserIdException : GameboardIntegrationTestException
{
    public CantResolveAuthUserIdException(StringValues userIds) : base($"Couldn't resolve the authenticated user ID. Found: {userIds}") { }
}
