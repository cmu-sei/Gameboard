namespace Gameboard.Tests.Integration.Fixtures;

internal class GameboardIntegrationTestException : Exception
{
    public GameboardIntegrationTestException(string message, Exception? innerException = null) : base(message, innerException) { }
}

internal class MissingServiceException<T> : GameboardIntegrationTestException where T : class
{
    public MissingServiceException(string? addlMessage)
        : base($"Couldn't resolve a required service of type {typeof(T).Name}.{(string.IsNullOrWhiteSpace(addlMessage) ? string.Empty : addlMessage)}") { }
}

internal class ResponseContentDeserializationTypeFailure<T> : GameboardIntegrationTestException
{
    public ResponseContentDeserializationTypeFailure(string responseContent, Exception innerException)
        : base($"Attempted to deserialize a response body to type {typeof(T).Name} but failed. \n\nResponse body: \"{responseContent}\"\nInner exception: {innerException.Message}") { }
}

internal class ResponseContentEmpty : GameboardIntegrationTestException
{
    public ResponseContentEmpty() : base("Deserialization failed because the body of the response is empty.") { }
}
