using System;

namespace Gameboard.Api.Common;

public class GameboardException : Exception
{
    internal GameboardException(string message) : base(message) { }
    internal GameboardException(string message, Exception innerException) : base(message, innerException) { }
}

internal class AppUrlResolutionException : GameboardException
{
    public AppUrlResolutionException()
        : base($"Unable to resolve the root URL of the application") { }
}

internal class SemaphoreLockFailure : GameboardException
{
    public SemaphoreLockFailure(Exception ex) : base($"An operation inside a semaphore lock failed.", ex) { }
}

internal class ResponseContentDeserializationTypeFailure<T> : GameboardException
{
    public ResponseContentDeserializationTypeFailure(string responseContent, Exception innerException)
        : base($"Attempted to deserialize a response body to type {typeof(T).Name} but failed. \n\nResponse body: \"{responseContent}\"\nInner exception: {innerException.Message}") { }
}
