namespace Gameboard.Tests.Integration.Fixtures;

internal class GameboardIntegrationTestException : Exception
{
    public GameboardIntegrationTestException(string message, Exception? innerException = null) : base(message, innerException) { }
}

internal class ResponseContentDeserializationTypeFailure<T> : GameboardIntegrationTestException
{
    public ResponseContentDeserializationTypeFailure(string responseContent)
        : base($"Attempted to deserialize a response body to type {typeof(T).Name} but failed. Response body: \"{responseContent}\"") { }
}

internal class ResponseContentEmpty : GameboardIntegrationTestException
{
    public ResponseContentEmpty() : base("Deserialization failed because the body of the response is empty.") { }
}